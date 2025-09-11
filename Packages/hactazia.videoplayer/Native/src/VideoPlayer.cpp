#include "../include/VideoPlayer.h"
#include <chrono>
#include <thread>
#include <string>

extern "C"
{
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
#include <libavutil/imgutils.h>
#include <libavutil/opt.h>
#include <libavutil/channel_layout.h>
#include <libavutil/error.h>
#include <libswscale/swscale.h>
#include <libswresample/swresample.h>
}

// Define AV_ERROR_MAX_STRING_SIZE if not available
#ifndef AV_ERROR_MAX_STRING_SIZE
#define AV_ERROR_MAX_STRING_SIZE 64
#endif

// Cache size limits to prevent memory issues
static const size_t MAX_VIDEO_CACHE_SIZE = 100; // Maximum number of cached video frames
static const size_t MAX_AUDIO_CACHE_SIZE = 200; // Maximum number of cached audio frames

VideoPlayer::VideoPlayer() : formatContext(nullptr),
                             videoCodecContext(nullptr),
                             audioCodecContext(nullptr),
                             swsContext(nullptr),
                             swrContext(nullptr),
                             frame(nullptr),
                             videoFrame(nullptr),
                             audioFrame(nullptr),
                             packet(nullptr),
                             videoStreamIndex(-1),
                             audioStreamIndex(-1),
                             isPlaying(false),
                             isPaused(false),
                             shouldStop(false),
                             running(false),
                             currentTime(0.0),
                             duration(0.0),
                             playerState(PlayerState::UNINITIALIZED),
                             playerError(PlayerError::NONE),
                             errorMessage(""),
                             rgbBuffer(nullptr),
                             videoCallback(nullptr),
                             audioCallback(nullptr),
                             videoWidth(0),
                             videoHeight(0),
                             frameRate(0.0)
{
    // Initialize Unity-safe frame buffers
    unityVideoFrame.data = nullptr;
    unityVideoFrame.width = 0;
    unityVideoFrame.height = 0;
    unityVideoFrame.timestamp = 0.0;
    unityVideoFrame.valid = false;
    
    unityAudioFrame.data = nullptr;
    unityAudioFrame.size = 0;
    unityAudioFrame.sampleRate = 0;
    unityAudioFrame.channels = 0;
    unityAudioFrame.timestamp = 0.0;
    unityAudioFrame.valid = false;
}

VideoPlayer::~VideoPlayer()
{
    destroy();
}

void VideoPlayer::destroy()
{
    // --- Signal all threads to stop ---
    shouldStop = true;
    isPlaying = false;
    isPaused = false;
    running = false;

    // --- Notify all waiting threads ---
    {
        std::lock_guard<std::mutex> lock(decodingMutex);
        decodingCondition.notify_all();
    }
    {
        std::lock_guard<std::mutex> lock(queueMutex);
        queueCond.notify_all();
    }

    // --- Stop threads proprement ---
    if (decodingThread.joinable())
    {
        decodingThread.join();
    }
    if (readThread.joinable())
    {
        readThread.join();
    }

    // --- Vider et nettoyer la file de paquets ---
    {
        std::lock_guard<std::mutex> lock(queueMutex);
        while (!packetQueue.empty())
        {
            AVPacket *pkt = packetQueue.front();
            av_packet_free(&pkt);
            packetQueue.pop();
        }
    }

    // --- Free all FFmpeg resources ---
    if (swsContext)
    {
        sws_freeContext(swsContext);
        swsContext = nullptr;
    }

    if (swrContext)
    {
        swr_free(&swrContext);
        swrContext = nullptr;
    }

    if (frame)
    {
        av_frame_free(&frame);
        frame = nullptr;
    }

    if (videoFrame)
    {
        av_frame_free(&videoFrame);
        videoFrame = nullptr;
    }

    if (audioFrame)
    {
        av_frame_free(&audioFrame);
        audioFrame = nullptr;
    }

    if (packet)
    {
        av_packet_free(&packet);
        packet = nullptr;
    }

    // Protect codec context cleanup
    {
        std::lock_guard<std::mutex> codecLock(codecMutex);
        
        if (videoCodecContext)
        {
            avcodec_free_context(&videoCodecContext);
            videoCodecContext = nullptr;
        }
        
        if (audioCodecContext)
        {
            avcodec_free_context(&audioCodecContext);
            audioCodecContext = nullptr;
        }
    }

    if (formatContext)
    {
        avformat_close_input(&formatContext);
        formatContext = nullptr;
    }

    // --- Clean up cached frames ---
    {
        std::lock_guard<std::mutex> lock(cacheMutex);

        // Libérer la mémoire des frames vidéo cachées
        for (auto &[timestamp, videoFrame] : cachedVideoFrames)
        {
            if (videoFrame.data && videoFrame.valid)
            {
                try
                {
                    delete[] videoFrame.data;
                }
                catch (...)
                {
                    // Ignore deletion errors to prevent crashes during cleanup
                }
                videoFrame.data = nullptr;
                videoFrame.valid = false;
            }
        }
        cachedVideoFrames.clear();

        // Libérer la mémoire des frames audio cachées
        for (auto &[timestamp, audioFrame] : cachedAudioFrames)
        {
            if (audioFrame.data && audioFrame.valid)
            {
                try
                {
                    delete[] audioFrame.data;
                }
                catch (...)
                {
                    // Ignore deletion errors to prevent crashes during cleanup
                }
                audioFrame.data = nullptr;
                audioFrame.valid = false;
            }
        }
        cachedAudioFrames.clear();
    }

    // --- Nettoyage divers ---
    if (rgbBuffer)
    {
        av_free(rgbBuffer);
        rgbBuffer = nullptr;
    }

    // --- Reset all state variables ---
    videoStreamIndex = -1;
    audioStreamIndex = -1;
    currentTime = 0.0;
    duration = 0.0;
    frameRate = 0.0;
    videoWidth = 0;
    videoHeight = 0;
    playerState = PlayerState::UNINITIALIZED;
    playerError = PlayerError::NONE;
    errorMessage = "";
    videoCallback = nullptr;
    audioCallback = nullptr;
}

bool VideoPlayer::loadVideo(const char *url)
{
    // Validate input parameters
    if (!url || strlen(url) == 0)
    {
        playerState = PlayerState::ERROR;
        playerError = PlayerError::FILE_NOT_FOUND;
        errorMessage = "Invalid URL parameter";
        return false;
    }

    // Stop any currently running playback before loading new video
    if (isPlaying)
    {
        shouldStop = true;
        isPlaying = false;
        isPaused = false;
        
        // Notify threads to stop
        {
            std::lock_guard<std::mutex> lock(decodingMutex);
            decodingCondition.notify_all();
        }
        
        // Wait for threads to finish
        if (decodingThread.joinable())
        {
            decodingThread.join();
        }
    }

    cleanup();
    playerError = PlayerError::NONE;
    errorMessage = "";

    // Set timeout options for network URLs
    AVDictionary *options = nullptr;
    if (strstr(url, "http://") == url || strstr(url, "https://") == url)
    {
        // Set connection timeout to 30 seconds
        av_dict_set(&options, "timeout", "30000000", 0); // 30 seconds in microseconds
        // Set user agent for better compatibility
        av_dict_set(&options, "user_agent", "Hactazia VideoPlayer/1.0", 0);
        // Enable reconnection on network errors
        av_dict_set(&options, "reconnect", "1", 0);
        av_dict_set(&options, "reconnect_streamed", "1", 0);
    }

    // Ouvrir le fichier/stream avec timeout
    int ret = avformat_open_input(&formatContext, url, nullptr, &options);
    av_dict_free(&options); // Clean up options regardless of result
    
    if (ret < 0)
    {
        playerState = PlayerState::ERROR;
        playerError = PlayerError::FILE_NOT_FOUND;
        char error_buf[AV_ERROR_MAX_STRING_SIZE];
        av_strerror(ret, error_buf, sizeof(error_buf));
        errorMessage = "Failed to open video file: " + std::string(url) + " - " + std::string(error_buf);
        return false;
    }

    // Récupérer les informations du stream
    ret = avformat_find_stream_info(formatContext, nullptr);
    if (ret < 0)
    {
        cleanup();
        playerState = PlayerState::ERROR;
        playerError = PlayerError::INVALID_FORMAT;
        char error_buf[AV_ERROR_MAX_STRING_SIZE];
        av_strerror(ret, error_buf, sizeof(error_buf));
        errorMessage = "Failed to retrieve stream information from: " + std::string(url) + " - " + std::string(error_buf);
        return false;
    }

    // Trouver les streams vidéo et audio
    for (unsigned int i = 0; i < formatContext->nb_streams; i++)
    {
        if (formatContext->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO && videoStreamIndex == -1)
        {
            videoStreamIndex = i;
        }
        else if (formatContext->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_AUDIO && audioStreamIndex == -1)
        {
            audioStreamIndex = i;
        }
    }

    if (videoStreamIndex == -1)
    {
        cleanup();
        playerState = PlayerState::ERROR;
        playerError = PlayerError::INVALID_FORMAT;
        errorMessage = "No video stream found in file: " + std::string(url);
        return false;
    }

    // Initialiser le codec vidéo
    const AVCodec *videoCodec = avcodec_find_decoder(formatContext->streams[videoStreamIndex]->codecpar->codec_id);
    if (!videoCodec)
    {
        cleanup();
        playerState = PlayerState::ERROR;
        playerError = PlayerError::CODEC_ERROR;
        errorMessage = "Video codec not found or not supported";
        return false;
    }

    videoCodecContext = avcodec_alloc_context3(videoCodec);
    if (avcodec_parameters_to_context(videoCodecContext, formatContext->streams[videoStreamIndex]->codecpar) < 0)
    {
        cleanup();
        playerState = PlayerState::ERROR;
        playerError = PlayerError::CODEC_ERROR;
        errorMessage = "Failed to copy video codec parameters to context";
        return false;
    }

    if (avcodec_open2(videoCodecContext, videoCodec, nullptr) < 0)
    {
        cleanup();
        playerState = PlayerState::ERROR;
        playerError = PlayerError::CODEC_ERROR;
        errorMessage = "Failed to open video codec";
        return false;
    }

    // Validate video dimensions
    if (videoCodecContext->width <= 0 || videoCodecContext->height <= 0 ||
        videoCodecContext->width > 8192 || videoCodecContext->height > 8192)
    {
        cleanup();
        playerState = PlayerState::ERROR;
        playerError = PlayerError::INVALID_FORMAT;
        errorMessage = "Invalid video dimensions";
        return false;
    }

    // Initialiser le codec audio si disponible
    if (audioStreamIndex != -1)
    {
        const AVCodec *audioCodec = avcodec_find_decoder(formatContext->streams[audioStreamIndex]->codecpar->codec_id);
        if (audioCodec)
        {
            audioCodecContext = avcodec_alloc_context3(audioCodec);
            if (avcodec_parameters_to_context(audioCodecContext, formatContext->streams[audioStreamIndex]->codecpar) >= 0)
            {
                avcodec_open2(audioCodecContext, audioCodec, nullptr);
            }
        }
    }

    // Allouer les frames
    frame = av_frame_alloc();
    videoFrame = av_frame_alloc();
    audioFrame = av_frame_alloc();

    if (!frame || !videoFrame || !audioFrame)
    {
        cleanup();
        playerState = PlayerState::ERROR;
        playerError = PlayerError::MEMORY_ERROR;
        errorMessage = "Failed to allocate frames";
        return false;
    }

    // Récupérer les métadonnées
    duration = (double)formatContext->duration / AV_TIME_BASE;
    if (formatContext->streams[videoStreamIndex]->avg_frame_rate.den != 0)
    {
        frameRate = av_q2d(formatContext->streams[videoStreamIndex]->avg_frame_rate);
    }
    videoWidth = videoCodecContext->width;
    videoHeight = videoCodecContext->height;

    playerState = PlayerState::LOADED;
    currentTime = 0.0;
    errorMessage = "";

    return true;
}

void VideoPlayer::play()
{
    if (!formatContext || playerState == PlayerState::ERROR)
        return;

    isPaused = false;
    if (!isPlaying)
    {
        // Join the previous thread if it exists and is joinable
        if (decodingThread.joinable())
        {
            decodingThread.join();
        }
        
        // If the video has ended, seek back to the beginning
        if (playerState == PlayerState::STOPPED)
        {
            seek(0.0);
        }
        
        isPlaying = true;
        shouldStop = false;
        playerState = PlayerState::PLAYING;
        decodingThread = std::thread(&VideoPlayer::decodingLoop, this);
    }
    else if (playerState == PlayerState::PAUSED)
    {
        playerState = PlayerState::PLAYING;
    }
}

void VideoPlayer::pause()
{
    if (playerState == PlayerState::PLAYING)
    {
        std::lock_guard<std::mutex> lock(decodingMutex);
        isPaused = true;
        playerState = PlayerState::PAUSED;
        // Don't notify here as we want to pause
    }
}

void VideoPlayer::resume()
{
    if (playerState == PlayerState::PAUSED)
    {
        std::lock_guard<std::mutex> lock(decodingMutex);
        isPaused = false;
        playerState = PlayerState::PLAYING;
        decodingCondition.notify_all();
    }
}

void VideoPlayer::stop()
{
    if (isPlaying)
    {
        // Signaler l'arrêt dans un ordre spécifique pour éviter les deadlocks
        shouldStop = true;

        // Notifier le thread de décodage pour qu'il se réveille s'il est en pause
        {
            std::lock_guard<std::mutex> lock(decodingMutex);
            isPlaying = false;
            isPaused = false;
            playerState = PlayerState::STOPPED;
        }
        decodingCondition.notify_all();

        // Attendre que le thread se termine
        if (decodingThread.joinable())
        {
            decodingThread.join();
        }

        currentTime = 0.0;
    }
}

void VideoPlayer::seek(double time)
{
    if (!formatContext || playerState == PlayerState::ERROR)
        return;

    int64_t timestamp = (int64_t)(time * AV_TIME_BASE);
    if (av_seek_frame(formatContext, -1, timestamp, AVSEEK_FLAG_BACKWARD) >= 0)
    {
        // Protect codec buffer flushing
        {
            std::lock_guard<std::mutex> codecLock(codecMutex);
            
            if (videoCodecContext)
            {
                avcodec_flush_buffers(videoCodecContext);
            }
            if (audioCodecContext)
            {
                avcodec_flush_buffers(audioCodecContext);
            }
        }
        
        currentTime = time;

        // Vider le cache des frames
        std::lock_guard<std::mutex> lock(cacheMutex);
        cachedVideoFrames.clear();
        cachedAudioFrames.clear();
    }
}

VideoFrame *VideoPlayer::getVideoFrameAtTime(double time)
{
    std::lock_guard<std::mutex> cacheLock(cacheMutex);

    VideoFrame* sourceFrame = nullptr;
    double lastTimestamp = -1.0;
    
    // Find the frame with the highest timestamp that is still <= time
    for (auto& [timestamp, frame] : cachedVideoFrames)
    {
        if (timestamp <= time && timestamp > lastTimestamp)
        {
            lastTimestamp = timestamp;
            sourceFrame = &frame;
        }
    }

    // Validate the source frame
    if (sourceFrame == nullptr || 
        sourceFrame->data == nullptr || 
        sourceFrame->width <= 0 || sourceFrame->height <= 0 ||
        sourceFrame->width > 8192 || sourceFrame->height > 8192)
    {
        return nullptr;
    }

    // Copy frame data to Unity-safe buffer
    {
        std::lock_guard<std::mutex> unityLock(unityFrameMutex);
        
        // Calculate required buffer size
        size_t bufferSize = sourceFrame->width * sourceFrame->height * 4; // RGBA
        
        // Allocate or reallocate Unity buffer if needed
        if (unityVideoFrame.data == nullptr || 
            unityVideoFrame.width != sourceFrame->width || 
            unityVideoFrame.height != sourceFrame->height)
        {
            delete[] unityVideoFrame.data;
            unityVideoFrame.data = new uint8_t[bufferSize];
            unityVideoFrame.width = sourceFrame->width;
            unityVideoFrame.height = sourceFrame->height;
        }
        
        // Copy frame data safely
        memcpy(unityVideoFrame.data, sourceFrame->data, bufferSize);
        unityVideoFrame.timestamp = sourceFrame->timestamp;
        unityVideoFrame.valid = true;
        
        return &unityVideoFrame;
    }
}

AudioFrame *VideoPlayer::getAudioFrameAtTime(double time)
{
    std::lock_guard<std::mutex> cacheLock(cacheMutex);

    AudioFrame* sourceFrame = nullptr;
    double lastTimestamp = -1.0;
    
    // Find the frame with the highest timestamp that is still <= time
    for (auto& [timestamp, frame] : cachedAudioFrames)
    {
        if (timestamp <= time && timestamp > lastTimestamp)
        {
            lastTimestamp = timestamp;
            sourceFrame = &frame;
        }
    }

    // Validate the source frame
    if (sourceFrame == nullptr || 
        sourceFrame->data == nullptr || 
        sourceFrame->size <= 0)
    {
        return nullptr;
    }

    // Copy frame data to Unity-safe buffer
    {
        std::lock_guard<std::mutex> unityLock(unityFrameMutex);
        
        // Allocate or reallocate Unity buffer if needed
        if (unityAudioFrame.data == nullptr || 
            unityAudioFrame.size != sourceFrame->size)
        {
            delete[] unityAudioFrame.data;
            unityAudioFrame.data = new uint8_t[sourceFrame->size];
            unityAudioFrame.size = sourceFrame->size;
        }
        
        // Copy frame data safely
        memcpy(unityAudioFrame.data, sourceFrame->data, sourceFrame->size);
        unityAudioFrame.sampleRate = sourceFrame->sampleRate;
        unityAudioFrame.channels = sourceFrame->channels;
        unityAudioFrame.timestamp = sourceFrame->timestamp;
        unityAudioFrame.valid = true;
        
        return &unityAudioFrame;
    }
}

double VideoPlayer::getDuration() const
{
    return duration;
}

double VideoPlayer::getCurrentTime() const
{
    return currentTime;
}

int VideoPlayer::getVideoWidth() const
{
    return videoWidth;
}

int VideoPlayer::getVideoHeight() const
{
    return videoHeight;
}

double VideoPlayer::getFrameRate() const
{
    return frameRate;
}

void VideoPlayer::setVideoFrameCallback(VideoFrameCallback callback)
{
    videoCallback = callback;
}

void VideoPlayer::setAudioFrameCallback(AudioFrameCallback callback)
{
    audioCallback = callback;
}

void VideoPlayer::update()
{
    // Cette méthode peut être appelée depuis Unity pour traiter les frames en attente
    // Pour l'instant, nous n'avons pas de logique spécifique ici
}

PlayerState VideoPlayer::getPlayerState() const
{
    return playerState;
}

PlayerError VideoPlayer::getPlayerError() const
{
    return playerError;
}

const char *VideoPlayer::getPlayerErrorMessage() const
{
    if (!errorMessage.empty())
    {
        return errorMessage.c_str();
    }
    return "";
}

size_t VideoPlayer::getVideoCacheSize() const
{
    std::lock_guard<std::mutex> lock(cacheMutex);
    return cachedVideoFrames.size();
}

size_t VideoPlayer::getAudioCacheSize() const
{
    std::lock_guard<std::mutex> lock(cacheMutex);
    return cachedAudioFrames.size();
}

double VideoPlayer::getLastVideoCacheTime() const
{
    std::lock_guard<std::mutex> lock(cacheMutex);
    if (cachedVideoFrames.empty())
    {
        return -1.0; // Indicate no frames cached
    }
    // Since std::map is ordered by key, the last element has the highest timestamp
    return cachedVideoFrames.rbegin()->first;
}

double VideoPlayer::getLastAudioCacheTime() const
{
    std::lock_guard<std::mutex> lock(cacheMutex);
    if (cachedAudioFrames.empty())
    {
        return -1.0; // Indicate no frames cached
    }
    // Since std::map is ordered by key, the last element has the highest timestamp
    return cachedAudioFrames.rbegin()->first;
}

const char* VideoPlayer::getFFMPEGDetails() const
{
    // Build debug information string with FFmpeg and player details
    ffmpegDebugDetails = "=== FFmpeg Debug Information ===\n";
    
    // FFmpeg library versions
    ffmpegDebugDetails += "FFmpeg Library Versions:\n";
    ffmpegDebugDetails += "  libavcodec: " + std::string(AV_STRINGIFY(LIBAVCODEC_VERSION)) + "\n";
    ffmpegDebugDetails += "  libavformat: " + std::string(AV_STRINGIFY(LIBAVFORMAT_VERSION)) + "\n";
    ffmpegDebugDetails += "  libavutil: " + std::string(AV_STRINGIFY(LIBAVUTIL_VERSION)) + "\n";
    ffmpegDebugDetails += "  libswscale: " + std::string(AV_STRINGIFY(LIBSWSCALE_VERSION)) + "\n";
    ffmpegDebugDetails += "  libswresample: " + std::string(AV_STRINGIFY(LIBSWRESAMPLE_VERSION)) + "\n\n";
    
    // Player state information
    ffmpegDebugDetails += "Player State:\n";
    ffmpegDebugDetails += "  State: ";
    switch (playerState.load()) {
        case PlayerState::UNINITIALIZED: ffmpegDebugDetails += "UNINITIALIZED"; break;
        case PlayerState::LOADED: ffmpegDebugDetails += "LOADED"; break;
        case PlayerState::PLAYING: ffmpegDebugDetails += "PLAYING"; break;
        case PlayerState::PAUSED: ffmpegDebugDetails += "PAUSED"; break;
        case PlayerState::STOPPED: ffmpegDebugDetails += "STOPPED"; break;
        case PlayerState::ERROR: ffmpegDebugDetails += "ERROR"; break;
        default: ffmpegDebugDetails += "UNKNOWN"; break;
    }
    ffmpegDebugDetails += "\n";
    
    ffmpegDebugDetails += "  Error: ";
    switch (playerError.load()) {
        case PlayerError::NONE: ffmpegDebugDetails += "NONE"; break;
        case PlayerError::FILE_NOT_FOUND: ffmpegDebugDetails += "FILE_NOT_FOUND"; break;
        case PlayerError::INVALID_FORMAT: ffmpegDebugDetails += "INVALID_FORMAT"; break;
        case PlayerError::CODEC_ERROR: ffmpegDebugDetails += "CODEC_ERROR"; break;
        case PlayerError::MEMORY_ERROR: ffmpegDebugDetails += "MEMORY_ERROR"; break;
        case PlayerError::UNKNOWN_ERROR: ffmpegDebugDetails += "UNKNOWN_ERROR"; break;
        default: ffmpegDebugDetails += "UNDEFINED"; break;
    }
    ffmpegDebugDetails += "\n";
    
    if (!errorMessage.empty()) {
        ffmpegDebugDetails += "  Error Message: " + errorMessage + "\n";
    }
    
    ffmpegDebugDetails += "  Is Playing: " + std::string(isPlaying ? "true" : "false") + "\n";
    ffmpegDebugDetails += "  Is Paused: " + std::string(isPaused ? "true" : "false") + "\n";
    ffmpegDebugDetails += "  Current Time: " + std::to_string(currentTime.load()) + "s\n";
    ffmpegDebugDetails += "  Duration: " + std::to_string(duration.load()) + "s\n\n";
    
    // Format context information
    if (formatContext) {
        ffmpegDebugDetails += "Format Context:\n";
        if (formatContext->url) {
            ffmpegDebugDetails += "  URL: " + std::string(formatContext->url) + "\n";
        }
        if (formatContext->iformat) {
            if (formatContext->iformat->name) {
                ffmpegDebugDetails += "  Format Name: " + std::string(formatContext->iformat->name) + "\n";
            }
            if (formatContext->iformat->long_name) {
                ffmpegDebugDetails += "  Format Long Name: " + std::string(formatContext->iformat->long_name) + "\n";
            }
            if (formatContext->iformat->extensions) {
                ffmpegDebugDetails += "  Extensions: " + std::string(formatContext->iformat->extensions) + "\n";
            }
            if (formatContext->iformat->mime_type) {
                ffmpegDebugDetails += "  MIME Type: " + std::string(formatContext->iformat->mime_type) + "\n";
            }
            ffmpegDebugDetails += "  Format Flags: 0x" + std::to_string(formatContext->iformat->flags) + "\n";
        }
        
        ffmpegDebugDetails += "  Streams: " + std::to_string(formatContext->nb_streams) + "\n";
        ffmpegDebugDetails += "  Duration: " + std::to_string(formatContext->duration / AV_TIME_BASE) + "s (" + std::to_string(formatContext->duration) + " time units)\n";
        ffmpegDebugDetails += "  Start Time: " + std::to_string(formatContext->start_time / AV_TIME_BASE) + "s\n";
        ffmpegDebugDetails += "  Bitrate: " + std::to_string(formatContext->bit_rate) + " bps\n";
        ffmpegDebugDetails += "  Packet Size: " + std::to_string(formatContext->packet_size) + "\n";
        ffmpegDebugDetails += "  Max Delay: " + std::to_string(formatContext->max_delay) + "\n";
        ffmpegDebugDetails += "  Flags: 0x" + std::to_string(formatContext->flags) + "\n";
        
        if (formatContext->nb_programs > 0) {
            ffmpegDebugDetails += "  Programs: " + std::to_string(formatContext->nb_programs) + "\n";
        }
        
        if (formatContext->nb_chapters > 0) {
            ffmpegDebugDetails += "  Chapters: " + std::to_string(formatContext->nb_chapters) + "\n";
        }
        
        // Stream information
        ffmpegDebugDetails += "  Stream Details:\n";
        for (unsigned int i = 0; i < formatContext->nb_streams; i++) {
            AVStream* stream = formatContext->streams[i];
            if (stream) {
                ffmpegDebugDetails += "    Stream " + std::to_string(i) + ":\n";
                ffmpegDebugDetails += "      Index: " + std::to_string(stream->index) + "\n";
                ffmpegDebugDetails += "      ID: " + std::to_string(stream->id) + "\n";
                
                if (stream->codecpar) {
                    ffmpegDebugDetails += "      Media Type: ";
                    switch (stream->codecpar->codec_type) {
                        case AVMEDIA_TYPE_VIDEO: ffmpegDebugDetails += "VIDEO"; break;
                        case AVMEDIA_TYPE_AUDIO: ffmpegDebugDetails += "AUDIO"; break;
                        case AVMEDIA_TYPE_SUBTITLE: ffmpegDebugDetails += "SUBTITLE"; break;
                        case AVMEDIA_TYPE_DATA: ffmpegDebugDetails += "DATA"; break;
                        case AVMEDIA_TYPE_ATTACHMENT: ffmpegDebugDetails += "ATTACHMENT"; break;
                        default: ffmpegDebugDetails += "UNKNOWN(" + std::to_string(stream->codecpar->codec_type) + ")"; break;
                    }
                    ffmpegDebugDetails += "\n";
                    
                    const AVCodecDescriptor* desc = avcodec_descriptor_get(stream->codecpar->codec_id);
                    if (desc && desc->name) {
                        ffmpegDebugDetails += "      Codec: " + std::string(desc->name) + "\n";
                    }
                    
                    if (stream->codecpar->codec_type == AVMEDIA_TYPE_VIDEO) {
                        ffmpegDebugDetails += "      Resolution: " + std::to_string(stream->codecpar->width) + "x" + std::to_string(stream->codecpar->height) + "\n";
                        if (stream->avg_frame_rate.num > 0 && stream->avg_frame_rate.den > 0) {
                            double fps = (double)stream->avg_frame_rate.num / stream->avg_frame_rate.den;
                            ffmpegDebugDetails += "      Frame Rate: " + std::to_string(fps) + " fps\n";
                        }
                        const char* pix_fmt_name = av_get_pix_fmt_name((AVPixelFormat)stream->codecpar->format);
                        if (pix_fmt_name) {
                            ffmpegDebugDetails += "      Pixel Format: " + std::string(pix_fmt_name) + "\n";
                        }
                    } else if (stream->codecpar->codec_type == AVMEDIA_TYPE_AUDIO) {
                        ffmpegDebugDetails += "      Sample Rate: " + std::to_string(stream->codecpar->sample_rate) + " Hz\n";
                        ffmpegDebugDetails += "      Channels: " + std::to_string(stream->codecpar->ch_layout.nb_channels) + "\n";
                        const char* sample_fmt_name = av_get_sample_fmt_name((AVSampleFormat)stream->codecpar->format);
                        if (sample_fmt_name) {
                            ffmpegDebugDetails += "      Sample Format: " + std::string(sample_fmt_name) + "\n";
                        }
                    }
                    
                    if (stream->codecpar->bit_rate > 0) {
                        ffmpegDebugDetails += "      Bitrate: " + std::to_string(stream->codecpar->bit_rate) + " bps\n";
                    }
                }
                
                if (stream->time_base.num > 0 && stream->time_base.den > 0) {
                    ffmpegDebugDetails += "      Time Base: " + std::to_string(stream->time_base.num) + "/" + std::to_string(stream->time_base.den) + "\n";
                }
                
                if (stream->start_time != AV_NOPTS_VALUE) {
                    double start_time = (double)stream->start_time * stream->time_base.num / stream->time_base.den;
                    ffmpegDebugDetails += "      Start Time: " + std::to_string(start_time) + "s\n";
                }
                
                if (stream->duration != AV_NOPTS_VALUE) {
                    double duration = (double)stream->duration * stream->time_base.num / stream->time_base.den;
                    ffmpegDebugDetails += "      Duration: " + std::to_string(duration) + "s\n";
                }
                
                if (stream->nb_frames > 0) {
                    ffmpegDebugDetails += "      Frame Count: " + std::to_string(stream->nb_frames) + "\n";
                }
                
                // Stream metadata
                if (stream->metadata) {
                    AVDictionaryEntry* entry = nullptr;
                    bool hasMetadata = false;
                    while ((entry = av_dict_get(stream->metadata, "", entry, AV_DICT_IGNORE_SUFFIX))) {
                        if (!hasMetadata) {
                            ffmpegDebugDetails += "      Metadata:\n";
                            hasMetadata = true;
                        }
                        ffmpegDebugDetails += "        " + std::string(entry->key) + ": " + std::string(entry->value) + "\n";
                    }
                }
            }
        }
        
        // Global metadata
        if (formatContext->metadata) {
            AVDictionaryEntry* entry = nullptr;
            bool hasMetadata = false;
            while ((entry = av_dict_get(formatContext->metadata, "", entry, AV_DICT_IGNORE_SUFFIX))) {
                if (!hasMetadata) {
                    ffmpegDebugDetails += "  Global Metadata:\n";
                    hasMetadata = true;
                }
                ffmpegDebugDetails += "    " + std::string(entry->key) + ": " + std::string(entry->value) + "\n";
            }
        }
        
        ffmpegDebugDetails += "\n";
    } else {
        ffmpegDebugDetails += "Format Context: NULL\n\n";
    }
    
    // Video codec information
    if (videoCodecContext) {
        ffmpegDebugDetails += "Video Codec:\n";
        if (videoCodecContext->codec && videoCodecContext->codec->name) {
            ffmpegDebugDetails += "  Codec: " + std::string(videoCodecContext->codec->name) + "\n";
        }
        ffmpegDebugDetails += "  Resolution: " + std::to_string(videoCodecContext->width) + "x" + std::to_string(videoCodecContext->height) + "\n";
        ffmpegDebugDetails += "  Pixel Format: " + std::string(av_get_pix_fmt_name(videoCodecContext->pix_fmt)) + "\n";
        ffmpegDebugDetails += "  Bitrate: " + std::to_string(videoCodecContext->bit_rate) + "\n";
        ffmpegDebugDetails += "  Frame Rate: " + std::to_string(frameRate) + " fps\n\n";
    }
    
    // Audio codec information
    if (audioCodecContext) {
        ffmpegDebugDetails += "Audio Codec:\n";
        if (audioCodecContext->codec && audioCodecContext->codec->name) {
            ffmpegDebugDetails += "  Codec: " + std::string(audioCodecContext->codec->name) + "\n";
        }
        ffmpegDebugDetails += "  Sample Rate: " + std::to_string(audioCodecContext->sample_rate) + " Hz\n";
        ffmpegDebugDetails += "  Channels: " + std::to_string(audioCodecContext->ch_layout.nb_channels) + "\n";
        ffmpegDebugDetails += "  Sample Format: " + std::string(av_get_sample_fmt_name(audioCodecContext->sample_fmt)) + "\n";
        ffmpegDebugDetails += "  Bitrate: " + std::to_string(audioCodecContext->bit_rate) + "\n\n";
    }
    
    // Cache information
    {
        std::lock_guard<std::mutex> lock(cacheMutex);
        ffmpegDebugDetails += "Cache Information:\n";
        ffmpegDebugDetails += "  Video Cache Size: " + std::to_string(cachedVideoFrames.size()) + "/" + std::to_string(MAX_VIDEO_CACHE_SIZE) + "\n";
        ffmpegDebugDetails += "  Audio Cache Size: " + std::to_string(cachedAudioFrames.size()) + "/" + std::to_string(MAX_AUDIO_CACHE_SIZE) + "\n";
        
        if (!cachedVideoFrames.empty()) {
            ffmpegDebugDetails += "  Video Cache Range: " + std::to_string(cachedVideoFrames.begin()->first) + "s - " + std::to_string(cachedVideoFrames.rbegin()->first) + "s\n";
        }
        if (!cachedAudioFrames.empty()) {
            ffmpegDebugDetails += "  Audio Cache Range: " + std::to_string(cachedAudioFrames.begin()->first) + "s - " + std::to_string(cachedAudioFrames.rbegin()->first) + "s\n";
        }
    }
    
    return ffmpegDebugDetails.c_str();
}

void VideoPlayer::cleanup()
{
    // Arrêter le thread de décodage s'il est en cours
    if (isPlaying)
    {
        shouldStop = true;
        {
            std::lock_guard<std::mutex> lock(decodingMutex);
            isPlaying = false;
            isPaused = false;
        }
        decodingCondition.notify_all();

        if (decodingThread.joinable())
        {
            decodingThread.join();
        }
    }

    // Libérer les ressources FFmpeg avec vérifications défensives
    if (swsContext)
    {
        sws_freeContext(swsContext);
        swsContext = nullptr;
    }

    if (swrContext)
    {
        swr_free(&swrContext);
        swrContext = nullptr;
    }

    if (frame)
    {
        av_frame_free(&frame);
        frame = nullptr;
    }

    if (videoFrame)
    {
        av_frame_free(&videoFrame);
        videoFrame = nullptr;
    }

    if (audioFrame)
    {
        av_frame_free(&audioFrame);
        audioFrame = nullptr;
    }

    // Protect codec context cleanup
    {
        std::lock_guard<std::mutex> codecLock(codecMutex);
        
        if (videoCodecContext)
        {
            avcodec_free_context(&videoCodecContext);
            videoCodecContext = nullptr;
        }

        if (audioCodecContext)
        {
            avcodec_free_context(&audioCodecContext);
            audioCodecContext = nullptr;
        }
    }

    if (formatContext)
    {
        avformat_close_input(&formatContext);
        formatContext = nullptr;
    }

    // Vider les caches et libérer la mémoire des frames cachées
    {
        std::lock_guard<std::mutex> lock(cacheMutex);

        // Libérer la mémoire des frames vidéo cachées
        for (auto &[timestamp, videoFrame] : cachedVideoFrames)
        {
            if (videoFrame.data && videoFrame.valid)
            {
                try
                {
                    delete[] videoFrame.data;
                }
                catch (...)
                {
                    // Ignore deletion errors to prevent crashes during cleanup
                }
                videoFrame.data = nullptr;
                videoFrame.valid = false;
            }
        }
        cachedVideoFrames.clear();

        // Libérer la mémoire des frames audio cachées
        for (auto &[timestamp, audioFrame] : cachedAudioFrames)
        {
            if (audioFrame.data && audioFrame.valid)
            {
                try
                {
                    delete[] audioFrame.data;
                }
                catch (...)
                {
                    // Ignore deletion errors to prevent crashes during cleanup
                }
                audioFrame.data = nullptr;
                audioFrame.valid = false;
            }
        }
        cachedAudioFrames.clear();
    }

    // Clean up Unity-safe frame buffers
    {
        std::lock_guard<std::mutex> unityLock(unityFrameMutex);
        
        if (unityVideoFrame.data != nullptr) {
            delete[] unityVideoFrame.data;
            unityVideoFrame.data = nullptr;
        }
        unityVideoFrame.width = 0;
        unityVideoFrame.height = 0;
        unityVideoFrame.valid = false;
        
        if (unityAudioFrame.data != nullptr) {
            delete[] unityAudioFrame.data;
            unityAudioFrame.data = nullptr;
        }
        unityAudioFrame.size = 0;
        unityAudioFrame.valid = false;
    }

    // Réinitialiser les variables
    videoStreamIndex = -1;
    audioStreamIndex = -1;
    isPlaying = false;
    isPaused = false;
    shouldStop = false;
    currentTime = 0.0;
    duration = 0.0;
    frameRate = 0.0;
    videoWidth = 0;
    videoHeight = 0;
    playerState = PlayerState::UNINITIALIZED;
    playerError = PlayerError::NONE;
    errorMessage = "";
}

void VideoPlayer::decodingLoop()
{
    // Validate essential components before starting
    if (!formatContext || !videoCodecContext || !frame)
    {
        playerState = PlayerState::ERROR;
        playerError = PlayerError::UNKNOWN_ERROR;
        errorMessage = "Invalid player state for decoding";
        isPlaying = false;
        return;
    }

    AVPacket *packet = av_packet_alloc();
    if (!packet)
    {
        playerState = PlayerState::ERROR;
        playerError = PlayerError::MEMORY_ERROR;
        errorMessage = "Failed to allocate packet";
        isPlaying = false;
        return;
    }

    try 
    {
        while (!shouldStop && isPlaying)
        {
            // Additional safety check - ensure essential components are still valid
            if (!formatContext || !videoCodecContext || !frame)
            {
                playerState = PlayerState::ERROR;
                isPlaying = false;
                break;
            }

            // Vérifier si on est en pause
            {
                std::unique_lock<std::mutex> lock(decodingMutex);
                decodingCondition.wait(lock, [this]
                                       { return !isPaused || shouldStop; });

                if (shouldStop)
                    break;
            }

            // Lire un paquet with timeout protection
            int ret = av_read_frame(formatContext, packet);
            if (ret >= 0)
            {
                try 
                {
                    if (packet->stream_index == videoStreamIndex)
                    {
                        decodeVideoPacket(packet);
                    }
                    else if (packet->stream_index == audioStreamIndex && audioCodecContext)
                    {
                        decodeAudioPacket(packet);
                    }
                    av_packet_unref(packet);
                }
                catch (...)
                {
                    // Handle any exceptions during packet processing
                    av_packet_unref(packet);
                    // Continue to next packet instead of crashing
                }
            }
            else
            {
                // Check if it's EOF or an error
                if (ret == AVERROR_EOF)
                {
                    // End of file reached
                    playerState = PlayerState::STOPPED;
                    isPlaying = false;
                    break;
                }
                else
                {
                    // Network error or other issue - try to continue for a bit
                    static int errorCount = 0;
                    errorCount++;
                    
                    if (errorCount > 10) // Too many consecutive errors
                    {
                        playerState = PlayerState::ERROR;
                        playerError = PlayerError::UNKNOWN_ERROR;
                        char error_buf[AV_ERROR_MAX_STRING_SIZE];
                        av_strerror(ret, error_buf, sizeof(error_buf));
                        errorMessage = "Decoding error: " + std::string(error_buf);
                        isPlaying = false;
                        break;
                    }
                    
                    // Small delay before retrying
                    std::this_thread::sleep_for(std::chrono::milliseconds(10));
                }
            }
        }
    }
    catch (...)
    {
        // Catch any unhandled exceptions to prevent crash
        playerState = PlayerState::ERROR;
        playerError = PlayerError::UNKNOWN_ERROR;
        errorMessage = "Unexpected error in decoding loop";
        isPlaying = false;
    }

    // Clean up packet
    if (packet)
    {
        av_packet_free(&packet);
    }
}

void VideoPlayer::decodeVideoPacket(AVPacket *packet)
{
    if (shouldStop || !packet)
        return;

    // Lock codec context access
    std::lock_guard<std::mutex> codecLock(codecMutex);
    
    // Double-check after acquiring lock
    if (!frame || !videoCodecContext || shouldStop)
        return;

    if (avcodec_send_packet(videoCodecContext, packet) == 0)
    {
        while (avcodec_receive_frame(videoCodecContext, frame) == 0)
        {
            if (shouldStop)
                return;

            // Calculer le timestamp de la frame
            double timestamp = 0.0;
            if (frame->pts != AV_NOPTS_VALUE)
            {
                timestamp = frame->pts * av_q2d(formatContext->streams[videoStreamIndex]->time_base);
                
                // For streaming content, ensure timestamp is reasonable
                // If timestamp is extremely large (like for live streams), use elapsed time instead
                if (timestamp > 1e9) // More than ~31 years, likely invalid for regular content
                {
                    static auto startTime = std::chrono::steady_clock::now();
                    auto elapsed = std::chrono::steady_clock::now() - startTime;
                    timestamp = std::chrono::duration<double>(elapsed).count();
                }
            }
            else
            {
                // If no PTS available, estimate based on frame rate and frame count
                static int frameCount = 0;
                frameCount++;
                if (frameRate > 0.0)
                {
                    timestamp = frameCount / frameRate;
                }
            }

            currentTime = timestamp;

            // Control playback speed - wait for the appropriate time
            if (frameRate > 0.0) {
                static auto lastFrameTime = std::chrono::steady_clock::now();
                auto expectedFrameDuration = std::chrono::duration<double>(1.0 / frameRate);
                auto currentFrameTime = std::chrono::steady_clock::now();
                auto actualDuration = currentFrameTime - lastFrameTime;
                
                if (actualDuration < expectedFrameDuration) {
                    std::this_thread::sleep_for(expectedFrameDuration - actualDuration);
                }
                lastFrameTime = std::chrono::steady_clock::now();
            }

            // Convertir la frame au format RGBA
            if (!swsContext)
            {
                swsContext = sws_getContext(
                    videoCodecContext->width, videoCodecContext->height, videoCodecContext->pix_fmt,
                    videoCodecContext->width, videoCodecContext->height, AV_PIX_FMT_RGBA,
                    SWS_BILINEAR, nullptr, nullptr, nullptr);
            }

            if (swsContext)
            {
                // Validate video dimensions more strictly
                if (videoCodecContext->width <= 0 || videoCodecContext->height <= 0 ||
                    videoCodecContext->width > 4096 || videoCodecContext->height > 4096)
                {
                    continue; // Skip invalid frames with more conservative limits
                }

                // Additional check for reasonable aspect ratio to prevent malformed frames
                double aspectRatio = static_cast<double>(videoCodecContext->width) / static_cast<double>(videoCodecContext->height);
                if (aspectRatio < 0.1 || aspectRatio > 10.0)
                {
                    continue; // Skip frames with unreasonable aspect ratios
                }

                // Allouer un buffer pour les données RGBA avec validation
                int bufferSize = av_image_get_buffer_size(AV_PIX_FMT_RGBA, videoCodecContext->width, videoCodecContext->height, 1);
                if (bufferSize <= 0 || bufferSize > 67108864) // 64MB limit
                {
                    continue; // Skip if buffer size calculation failed or is too large
                }

                uint8_t *data = nullptr;
                try
                {
                    data = new uint8_t[bufferSize];
                }
                catch (const std::bad_alloc &)
                {
                    continue; // Skip if allocation fails
                }

                if (!data)
                {
                    continue;
                }

                // Calculate buffer offsets safely with overflow protection
                int lineSize = videoCodecContext->width * 4;
                if (lineSize <= 0 || lineSize > bufferSize)
                {
                    delete[] data;
                    continue;
                }

                // Check for potential overflow in offset calculation
                size_t offsetBytes = static_cast<size_t>(videoCodecContext->height - 1) * static_cast<size_t>(lineSize);
                if (offsetBytes >= bufferSize)
                {
                    delete[] data;
                    continue;
                }

                uint8_t *destData[1] = {data + offsetBytes};
                int destLinesize[1] = {-lineSize};

                // Validate that the destination pointer is within bounds
                if (destData[0] < data || destData[0] >= data + bufferSize)
                {
                    delete[] data;
                    continue;
                }

                int scaleResult = sws_scale(swsContext, frame->data, frame->linesize, 0, videoCodecContext->height, destData, destLinesize);
                if (scaleResult < 0)
                {
                    delete[] data;
                    continue; // Skip if scaling failed
                }

                // Créer une VideoFrame et l'ajouter au cache
                VideoFrame videoFrameData;
                videoFrameData.data = data;
                videoFrameData.width = videoCodecContext->width;
                videoFrameData.height = videoCodecContext->height;
                videoFrameData.timestamp = timestamp;
                videoFrameData.valid = true;

                {
                    std::lock_guard<std::mutex> lock(cacheMutex);

                    // Check if we already have a frame at this timestamp and clean it up first
                    auto existingIt = cachedVideoFrames.find(timestamp);
                    if (existingIt != cachedVideoFrames.end() && existingIt->second.data && existingIt->second.valid)
                    {
                        delete[] existingIt->second.data;
                        existingIt->second.data = nullptr;
                        existingIt->second.valid = false;
                    }

                    // Cache size management - remove oldest frames if cache is too large
                    while (cachedVideoFrames.size() >= MAX_VIDEO_CACHE_SIZE)
                    {
                        auto oldestIt = cachedVideoFrames.begin();
                        if (oldestIt->second.data && oldestIt->second.valid)
                        {
                            delete[] oldestIt->second.data;
                            oldestIt->second.data = nullptr;
                            oldestIt->second.valid = false;
                        }
                        cachedVideoFrames.erase(oldestIt);
                    }

                    cachedVideoFrames[timestamp] = videoFrameData;
                }

                // Appeler le callback si défini
                if (videoCallback)
                {
                    videoCallback(&videoFrameData);
                }
            }
        }
    }
}

void VideoPlayer::decodeAudioPacket(AVPacket *packet)
{
    if (shouldStop || !packet)
        return;
        
    // Lock codec context access
    std::lock_guard<std::mutex> codecLock(codecMutex);
    
    // Double-check after acquiring lock
    if (!frame || !audioCodecContext || shouldStop)
        return;
        
    if (avcodec_send_packet(audioCodecContext, packet) == 0)
    {
        while (avcodec_receive_frame(audioCodecContext, frame) == 0)
        {
            if (shouldStop)
                return;
            // Calculer le timestamp de la frame
            double timestamp = 0.0;
            if (frame->pts != AV_NOPTS_VALUE)
            {
                timestamp = frame->pts * av_q2d(formatContext->streams[audioStreamIndex]->time_base);
            }

            // Convertir l'audio si nécessaire
            if (!swrContext)
            {
                AVChannelLayout out_layout = AV_CHANNEL_LAYOUT_STEREO;
                AVChannelLayout in_layout;

                // Get input channel layout
                if (audioCodecContext->ch_layout.nb_channels > 0)
                {
                    in_layout = audioCodecContext->ch_layout;
                }
                else
                {
                    av_channel_layout_default(&in_layout, audioCodecContext->ch_layout.nb_channels);
                }

                swr_alloc_set_opts2(&swrContext,
                                    &out_layout, AV_SAMPLE_FMT_S16, 44100,
                                    &in_layout, audioCodecContext->sample_fmt, audioCodecContext->sample_rate,
                                    0, nullptr);
                swr_init(swrContext);
            }

            if (swrContext)
            {
                int outputSamples = swr_get_out_samples(swrContext, frame->nb_samples);

                // Validate output samples with stricter bounds
                if (outputSamples <= 0 || outputSamples > 100000)
                {             // More conservative upper limit
                    continue; // Skip invalid samples
                }

                // Calculate buffer size with overflow protection
                const size_t channels = 2;
                const size_t bytesPerSample = 2;
                
                // Check for multiplication overflow
                if (outputSamples > SIZE_MAX / (channels * bytesPerSample))
                {
                    continue; // Skip if multiplication would overflow
                }
                
                size_t bufferSize = static_cast<size_t>(outputSamples) * channels * bytesPerSample;
                if (bufferSize > 10000000)
                {             // 10MB limit (more conservative)
                    continue; // Skip if buffer would be too large
                }

                uint8_t *outputBuffer = nullptr;
                try
                {
                    outputBuffer = new uint8_t[bufferSize];
                }
                catch (const std::bad_alloc &)
                {
                    continue; // Skip if allocation fails
                }

                if (!outputBuffer)
                {
                    continue;
                }

                uint8_t *outputData[1] = {outputBuffer};
                int convertedSamples = swr_convert(swrContext, outputData, outputSamples, (const uint8_t **)frame->data, frame->nb_samples);

                if (convertedSamples <= 0)
                {
                    delete[] outputBuffer;
                    continue; // Skip if conversion failed
                }

                // Créer une AudioFrame et l'ajouter au cache
                AudioFrame audioFrameData;
                audioFrameData.data = outputBuffer;
                audioFrameData.size = static_cast<size_t>(convertedSamples) * channels * bytesPerSample;
                audioFrameData.sampleRate = 44100;
                audioFrameData.channels = 2;
                audioFrameData.timestamp = timestamp;
                audioFrameData.valid = true;

                {
                    std::lock_guard<std::mutex> lock(cacheMutex);

                    // Check if we already have a frame at this timestamp and clean it up first
                    auto existingIt = cachedAudioFrames.find(timestamp);
                    if (existingIt != cachedAudioFrames.end() && existingIt->second.data && existingIt->second.valid)
                    {
                        delete[] existingIt->second.data;
                        existingIt->second.data = nullptr;
                        existingIt->second.valid = false;
                    }

                    // Cache size management - remove oldest frames if cache is too large
                    while (cachedAudioFrames.size() >= MAX_AUDIO_CACHE_SIZE)
                    {
                        auto oldestIt = cachedAudioFrames.begin();
                        if (oldestIt->second.data && oldestIt->second.valid)
                        {
                            delete[] oldestIt->second.data;
                            oldestIt->second.data = nullptr;
                            oldestIt->second.valid = false;
                        }
                        cachedAudioFrames.erase(oldestIt);
                    }

                    cachedAudioFrames[timestamp] = audioFrameData;
                }

                // Appeler le callback si défini
                if (audioCallback)
                {
                    audioCallback(&audioFrameData);
                }
            }
        }
    }
}
