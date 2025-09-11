#ifndef VIDEOPLAYER_EXPORTS
#define VIDEOPLAYER_EXPORTS
#endif

#include "../include/VideoPlayer.h"
#include <unordered_map>
#include <map>
#include <thread>
#include <mutex>
#include <condition_variable>
#include <queue>
#include <atomic>
#include <chrono>
#include <algorithm>

class VideoPlayerImpl {
public:
    VideoPlayerImpl() : 
        formatContext(nullptr),
        videoCodecContext(nullptr),
        audioCodecContext(nullptr),
        swsContext(nullptr),
        swrContext(nullptr),
        frame(nullptr),
        videoFrame(nullptr),
        audioFrame(nullptr),
        videoStreamIndex(-1),
        audioStreamIndex(-1),
        isPlaying(false),
        isPaused(false),
        shouldStop(false),
        currentTime(0.0),
        duration(0.0),
        frameRate(0.0),
        playerState(PlayerState::UNINITIALIZED),
        playerError(PlayerError::NONE),
        playerErrorMessage(""),
        videoFrameCallback(nullptr),
        audioFrameCallback(nullptr) {
    }
    
    ~VideoPlayerImpl() {
        cleanup();
    }
    
    bool loadVideo(const char* url) {
        cleanup();
        playerError = PlayerError::NONE;
        playerErrorMessage = "";
        
        // Ouvrir le fichier/stream
        if (avformat_open_input(&formatContext, url, nullptr, nullptr) < 0) {
            playerState = PlayerState::ERROR;
            playerError = PlayerError::FILE_NOT_FOUND;
            playerErrorMessage = "Failed to open video file: " + std::string(url);
            return false;
        }
        
        // Récupérer les informations du stream
        if (avformat_find_stream_info(formatContext, nullptr) < 0) {
            cleanup();
            playerState = PlayerState::ERROR;
            playerError = PlayerError::INVALID_FORMAT;
            playerErrorMessage = "Failed to retrieve stream information from: " + std::string(url);
            return false;
        }
        
        // Trouver les streams vidéo et audio
        for (unsigned int i = 0; i < formatContext->nb_streams; i++) {
            if (formatContext->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO && videoStreamIndex == -1) {
                videoStreamIndex = i;
            } else if (formatContext->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_AUDIO && audioStreamIndex == -1) {
                audioStreamIndex = i;
            }
        }
        
        if (videoStreamIndex == -1) {
            cleanup();
            playerState = PlayerState::ERROR;
            playerError = PlayerError::INVALID_FORMAT;
            playerErrorMessage = "No video stream found in file: " + std::string(url);
            return false;
        }
        
        // Initialiser le codec vidéo
        const AVCodec* videoCodec = avcodec_find_decoder(formatContext->streams[videoStreamIndex]->codecpar->codec_id);
        if (!videoCodec) {
            cleanup();
            playerState = PlayerState::ERROR;
            playerError = PlayerError::CODEC_ERROR;
            playerErrorMessage = "Video codec not found or not supported";
            return false;
        }
        
        videoCodecContext = avcodec_alloc_context3(videoCodec);
        if (avcodec_parameters_to_context(videoCodecContext, formatContext->streams[videoStreamIndex]->codecpar) < 0) {
            cleanup();
            playerState = PlayerState::ERROR;
            playerError = PlayerError::CODEC_ERROR;
            playerErrorMessage = "Failed to copy video codec parameters to context";
            return false;
        }
        
        if (avcodec_open2(videoCodecContext, videoCodec, nullptr) < 0) {
            cleanup();
            playerState = PlayerState::ERROR;
            playerError = PlayerError::CODEC_ERROR;
            playerErrorMessage = "Failed to open video codec";
            return false;
        }
        
        // Initialiser le codec audio si présent
        if (audioStreamIndex != -1) {
            const AVCodec* audioCodec = avcodec_find_decoder(formatContext->streams[audioStreamIndex]->codecpar->codec_id);
            if (audioCodec) {
                audioCodecContext = avcodec_alloc_context3(audioCodec);
                if (avcodec_parameters_to_context(audioCodecContext, formatContext->streams[audioStreamIndex]->codecpar) >= 0) {
                    avcodec_open2(audioCodecContext, audioCodec, nullptr);
                }
            }
        }
        
        // Initialiser les contextes de conversion
        swsContext = sws_getContext(
            videoCodecContext->width, videoCodecContext->height, videoCodecContext->pix_fmt,
            videoCodecContext->width, videoCodecContext->height, AV_PIX_FMT_RGBA,
            SWS_BILINEAR, nullptr, nullptr, nullptr
        );
        
        if (audioCodecContext) {
            // For now, disable audio resampling to avoid compatibility issues
            // This can be re-enabled once the proper channel layout API is determined
            swrContext = nullptr;
        }
        
        // Allouer les frames
        frame = av_frame_alloc();
        videoFrame = av_frame_alloc();
        audioFrame = av_frame_alloc();
        
        // Calculer la durée et le framerate
        duration = static_cast<double>(formatContext->duration) / AV_TIME_BASE;
        AVRational frameRateRational = formatContext->streams[videoStreamIndex]->r_frame_rate;
        frameRate = av_q2d(frameRateRational);
        
        playerState = PlayerState::LOADED;
        playerError = PlayerError::NONE;
        playerErrorMessage = "";
        return true;
    }
    
    void play() {
        if (!formatContext || playerState == PlayerState::ERROR) return;
        
        isPaused = false;
        if (!isPlaying) {
            isPlaying = true;
            shouldStop = false;
            playerState = PlayerState::PLAYING;
            decodingThread = std::thread(&VideoPlayerImpl::decodingLoop, this);
        } else if (playerState == PlayerState::PAUSED) {
            playerState = PlayerState::PLAYING;
        }
    }
    
    void pause() {
        if (playerState == PlayerState::PLAYING) {
            {
                std::lock_guard<std::mutex> lock(decodingMutex);
                isPaused = true;
                playerState = PlayerState::PAUSED;
            }
            // Ne pas notifier ici car on veut mettre en pause
        }
    }
    
    void resume() {
        if (playerState == PlayerState::PAUSED) {
            {
                std::lock_guard<std::mutex> lock(decodingMutex);
                isPaused = false;
                playerState = PlayerState::PLAYING;
            }
            decodingCondition.notify_all();
        }
    }
    
    void stop() {
        if (isPlaying) {
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
            if (decodingThread.joinable()) {
                decodingThread.join();
            }
            
            // Vider le cache après que le thread soit arrêté
            std::lock_guard<std::mutex> lock(cacheMutex);
            for (auto& pair : videoCache) {
                delete[] pair.second.data;
            }
            for (auto& pair : audioCache) {
                delete[] pair.second.data;
            }
            videoCache.clear();
            audioCache.clear();
            currentTime = 0.0;
        }
    }
    
    void seek(double time) {
        if (!formatContext) return;
        
        int64_t timestamp = static_cast<int64_t>(time * AV_TIME_BASE);
        av_seek_frame(formatContext, -1, timestamp, AVSEEK_FLAG_BACKWARD);
        
        std::lock_guard<std::mutex> lock(cacheMutex);
        // Nettoyer le cache après le seek
        videoCache.erase(videoCache.upper_bound(time), videoCache.end());
        audioCache.erase(audioCache.upper_bound(time), audioCache.end());
        currentTime = time;
    }
    
    VideoFrame* getVideoFrameAtTime(double time) {
        std::lock_guard<std::mutex> lock(cacheMutex);
        
        // Chercher dans le cache
        auto it = videoCache.lower_bound(time);
        if (it != videoCache.end() && std::abs(it->first - time) < 0.033) { // 33ms de tolérance
            VideoFrame* result = new VideoFrame();
            *result = it->second;
            return result;
        }
        
        // Pas trouvé dans le cache
        VideoFrame* result = new VideoFrame();
        result->valid = false;
        return result;
    }
    
    AudioFrame* getAudioFrameAtTime(double time) {
        std::lock_guard<std::mutex> lock(cacheMutex);
        
        // Chercher dans le cache
        auto it = audioCache.lower_bound(time);
        if (it != audioCache.end() && std::abs(it->first - time) < 0.020) { // 20ms de tolérance
            AudioFrame* result = new AudioFrame();
            *result = it->second;
            return result;
        }
        
        // Pas trouvé dans le cache
        AudioFrame* result = new AudioFrame();
        result->valid = false;
        return result;
    }
    
    void update() {
        if (isPlaying && !isPaused) {
            auto now = std::chrono::steady_clock::now();
            auto elapsed = std::chrono::duration_cast<std::chrono::microseconds>(now - lastUpdateTime).count() / 1000000.0;
            currentTime.store(currentTime.load() + elapsed);
            lastUpdateTime = now;
        }
    }
    
    // Getters
    double getDuration() const { return duration; }
    double getCurrentTime() const { return currentTime; }
    int getVideoWidth() const { return videoCodecContext ? videoCodecContext->width : 0; }
    int getVideoHeight() const { return videoCodecContext ? videoCodecContext->height : 0; }
    double getFrameRate() const { return frameRate; }
    PlayerState getPlayerState() const { return playerState; }
    PlayerError getPlayerError() const { return playerError; }
    const char* getPlayerErrorMessage() const {
        if (!playerErrorMessage.empty()) {
            return playerErrorMessage.c_str();
        }
        
        switch (playerError) {
            case PlayerError::NONE:
                return "No error";
            case PlayerError::FILE_NOT_FOUND:
                return "File not found or cannot be opened";
            case PlayerError::INVALID_FORMAT:
                return "Invalid file format or no video stream found";
            case PlayerError::CODEC_ERROR:
                return "Codec initialization failed";
            case PlayerError::MEMORY_ERROR:
                return "Memory allocation failed";
            case PlayerError::UNKNOWN_ERROR:
            default:
                return "Unknown error occurred";
        }
    }
    
    void setPlayerErrorMessage(const std::string& message) { playerErrorMessage = message; }
    const std::string& getPlayerErrorMessageString() const { return playerErrorMessage; }
    
    // Callbacks
    void setVideoFrameCallback(VideoFrameCallback callback) { videoFrameCallback = callback; }
    void setAudioFrameCallback(AudioFrameCallback callback) { audioFrameCallback = callback; }

private:
    // Contextes FFmpeg
    AVFormatContext* formatContext;
    AVCodecContext* videoCodecContext;
    AVCodecContext* audioCodecContext;
    SwsContext* swsContext;
    SwrContext* swrContext;
    AVFrame* frame;
    AVFrame* videoFrame;
    AVFrame* audioFrame;
    
    // Indices des streams
    int videoStreamIndex;
    int audioStreamIndex;
    
    // État de lecture
    std::atomic<bool> isPlaying;
    std::atomic<bool> isPaused;
    std::atomic<bool> shouldStop;
    std::atomic<double> currentTime;
    double duration;
    double frameRate;
    
    // État et erreur
    std::atomic<PlayerState> playerState;
    std::atomic<PlayerError> playerError;
    std::string playerErrorMessage;
    
    // Thread de décodage
    std::thread decodingThread;
    std::mutex decodingMutex;
    std::condition_variable decodingCondition;
    
    // Cache des frames avec timestamp comme clé
    std::mutex cacheMutex;
    std::map<double, VideoFrame> videoCache;
    std::map<double, AudioFrame> audioCache;
    
    // Callbacks
    VideoFrameCallback videoFrameCallback;
    AudioFrameCallback audioFrameCallback;
    
    // Timing
    std::chrono::steady_clock::time_point lastUpdateTime;
    
    void decodingLoop() {
        AVPacket* packet = av_packet_alloc();
        
        lastUpdateTime = std::chrono::steady_clock::now();
        
        while (!shouldStop && av_read_frame(formatContext, packet) >= 0) {
            // Vérifier si on doit s'arrêter avant de traiter le packet
            if (shouldStop) {
                av_packet_unref(packet);
                break;
            }
            
            if (packet->stream_index == videoStreamIndex) {
                decodeVideoPacket(packet);
            } else if (packet->stream_index == audioStreamIndex) {
                decodeAudioPacket(packet);
            }
            
            av_packet_unref(packet);
            
            // Pause handling avec protection contre les conditions de course
            {
                std::unique_lock<std::mutex> lock(decodingMutex);
                while (isPaused.load() && !shouldStop.load()) {
                    decodingCondition.wait(lock);
                }
            }
        }
        
        av_packet_free(&packet);
    }
    
    void decodeVideoPacket(AVPacket* packet) {
        if (avcodec_send_packet(videoCodecContext, packet) < 0) return;
        
        while (avcodec_receive_frame(videoCodecContext, frame) >= 0) {
            // Convertir en RGBA
            uint8_t* data = new uint8_t[videoCodecContext->width * videoCodecContext->height * 4];
            uint8_t* destData[1] = { data };
            int destLinesize[1] = { videoCodecContext->width * 4 };
            
            sws_scale(swsContext, frame->data, frame->linesize, 0, videoCodecContext->height,
                     destData, destLinesize);
            
            // Calculer le timestamp
            double timestamp = static_cast<double>(frame->pts * av_q2d(formatContext->streams[videoStreamIndex]->time_base));
            
            // Ajouter au cache
            {
                std::lock_guard<std::mutex> lock(cacheMutex);
                VideoFrame vf;
                vf.data = data;
                vf.width = videoCodecContext->width;
                vf.height = videoCodecContext->height;
                vf.timestamp = timestamp;
                vf.valid = true;
                
                videoCache[timestamp] = vf;
                
                // Limiter la taille du cache (garder seulement les 100 dernières frames)
                if (videoCache.size() > 100) {
                    auto it = videoCache.begin();
                    delete[] it->second.data;
                    videoCache.erase(it);
                }
            }
            
            // Callback si défini
            if (videoFrameCallback) {
                VideoFrame vf;
                vf.data = data;
                vf.width = videoCodecContext->width;
                vf.height = videoCodecContext->height;
                vf.timestamp = timestamp;
                vf.valid = true;
                videoFrameCallback(&vf);
            }
        }
    }
    
    void decodeAudioPacket(AVPacket* packet) {
        // Skip audio processing if no resampler is available
        if (!audioCodecContext || !swrContext) return;
        if (avcodec_send_packet(audioCodecContext, packet) < 0) return;
        
        while (avcodec_receive_frame(audioCodecContext, frame) >= 0) {
            // Convertir en format standard
            int outputSamples = swr_get_out_samples(swrContext, frame->nb_samples);
            uint8_t* outputBuffer = new uint8_t[outputSamples * 2 * 2]; // 2 channels, 2 bytes per sample
            uint8_t* outputData[1] = { outputBuffer };
            
            int convertedSamples = swr_convert(swrContext, outputData, outputSamples,
                                             (const uint8_t**)frame->data, frame->nb_samples);
            
            if (convertedSamples > 0) {
                double timestamp = static_cast<double>(frame->pts * av_q2d(formatContext->streams[audioStreamIndex]->time_base));
                
                // Ajouter au cache
                {
                    std::lock_guard<std::mutex> lock(cacheMutex);
                    AudioFrame af;
                    af.data = outputBuffer;
                    af.size = convertedSamples * 2 * 2;
                    af.sampleRate = 44100;
                    af.channels = 2;
                    af.timestamp = timestamp;
                    af.valid = true;
                    
                    audioCache[timestamp] = af;
                    
                    // Limiter la taille du cache
                    if (audioCache.size() > 200) {
                        auto it = audioCache.begin();
                        delete[] it->second.data;
                        audioCache.erase(it);
                    }
                }
                
                // Callback si défini
                if (audioFrameCallback) {
                    AudioFrame af;
                    af.data = outputBuffer;
                    af.size = convertedSamples * 2 * 2;
                    af.sampleRate = 44100;
                    af.channels = 2;
                    af.timestamp = timestamp;
                    af.valid = true;
                    audioFrameCallback(&af);
                }
            } else {
                delete[] outputBuffer;
            }
        }
    }
    
    void cleanup() {
        stop();
        
        // Le cache est déjà nettoyé dans stop(), pas besoin de le faire ici
        // Juste s'assurer que le cache est vide au cas où stop() n'aurait pas été appelé
        {
            std::lock_guard<std::mutex> lock(cacheMutex);
            if (!videoCache.empty()) {
                for (auto& pair : videoCache) {
                    delete[] pair.second.data;
                }
                videoCache.clear();
            }
            
            if (!audioCache.empty()) {
                for (auto& pair : audioCache) {
                    delete[] pair.second.data;
                }
                audioCache.clear();
            }
        }
        
        // Libérer les contextes FFmpeg
        if (swsContext) {
            sws_freeContext(swsContext);
            swsContext = nullptr;
        }
        
        if (swrContext) {
            swr_free(&swrContext);
        }
        
        if (frame) {
            av_frame_free(&frame);
        }
        
        if (videoFrame) {
            av_frame_free(&videoFrame);
        }
        
        if (audioFrame) {
            av_frame_free(&audioFrame);
        }
        
        if (videoCodecContext) {
            avcodec_free_context(&videoCodecContext);
        }
        
        if (audioCodecContext) {
            avcodec_free_context(&audioCodecContext);
        }
        
        if (formatContext) {
            avformat_close_input(&formatContext);
        }
        
        videoStreamIndex = -1;
        audioStreamIndex = -1;
        currentTime = 0.0;
        duration = 0.0;
        frameRate = 0.0;
        playerState = PlayerState::UNINITIALIZED;
        playerError = PlayerError::NONE;
        playerErrorMessage = "";
    }
};

// Fonctions d'implémentation (internes, pas d'export)

VideoPlayerImpl* CreateVideoPlayerImpl() {
    return new VideoPlayerImpl();
}

void DestroyVideoPlayerImpl(VideoPlayerImpl* impl) {
    delete impl;
}

bool LoadVideoImpl(VideoPlayerImpl* impl, const char* url) {
    return impl->loadVideo(url);
}

void PlayImpl(VideoPlayerImpl* impl) {
    impl->play();
}

void PauseImpl(VideoPlayerImpl* impl) {
    impl->pause();
}

void ResumeImpl(VideoPlayerImpl* impl) {
    impl->resume();
}

void StopImpl(VideoPlayerImpl* impl) {
    impl->stop();
}

void SeekImpl(VideoPlayerImpl* impl, double time) {
    impl->seek(time);
}

VideoFrame* GetVideoFrameAtTimeImpl(VideoPlayerImpl* impl, double time) {
    return impl->getVideoFrameAtTime(time);
}

AudioFrame* GetAudioFrameAtTimeImpl(VideoPlayerImpl* impl, double time) {
    return impl->getAudioFrameAtTime(time);
}

double GetDurationImpl(VideoPlayerImpl* impl) {
    return impl->getDuration();
}

double GetCurrentTimeImpl(VideoPlayerImpl* impl) {
    return impl->getCurrentTime();
}

int GetVideoWidthImpl(VideoPlayerImpl* impl) {
    return impl->getVideoWidth();
}

int GetVideoHeightImpl(VideoPlayerImpl* impl) {
    return impl->getVideoHeight();
}

double GetFrameRateImpl(VideoPlayerImpl* impl) {
    return impl->getFrameRate();
}

void SetVideoFrameCallbackImpl(VideoPlayerImpl* impl, VideoFrameCallback callback) {
    impl->setVideoFrameCallback(callback);
}

void SetAudioFrameCallbackImpl(VideoPlayerImpl* impl, AudioFrameCallback callback) {
    impl->setAudioFrameCallback(callback);
}

void UpdatePlayerImpl(VideoPlayerImpl* impl) {
    impl->update();
}

int GetPlayerStateImpl(VideoPlayerImpl* impl) {
    return static_cast<int>(impl->getPlayerState());
}

int GetPlayerErrorImpl(VideoPlayerImpl* impl) {
    return static_cast<int>(impl->getPlayerError());
}

const char* GetPlayerErrorMessageImpl(VideoPlayerImpl* impl) {
    return impl->getPlayerErrorMessage();
}
