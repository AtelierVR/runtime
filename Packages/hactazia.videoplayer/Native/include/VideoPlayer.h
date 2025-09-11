#pragma once

#include <thread>
#include <atomic>
#include <mutex>
#include <condition_variable>
#include <queue>
#include <map>
#include <functional>

extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
#include <libavutil/imgutils.h>
#include <libavutil/opt.h>
#include <libavutil/channel_layout.h>
#include <libswscale/swscale.h>
#include <libswresample/swresample.h>
#include "hactazia_videoplayer.h"
}

class VideoPlayer {
public:
    VideoPlayer();
    ~VideoPlayer();

    bool loadVideo(const char* url);
    void play();
    void pause();
    void resume();
    void stop();
    void seek(double time);
    VideoFrame* getVideoFrameAtTime(double time);
    AudioFrame* getAudioFrameAtTime(double time);
    double getDuration() const;
    double getCurrentTime() const;
    int getVideoWidth() const;
    int getVideoHeight() const;
    double getFrameRate() const;
    void setVideoFrameCallback(VideoFrameCallback callback);
    void setAudioFrameCallback(AudioFrameCallback callback);
    void update();
    PlayerState getPlayerState() const;
    PlayerError getPlayerError() const;
    const char* getPlayerErrorMessage() const;
    void destroy(); // Add missing destroy method

private:
    void cleanup();
    void decodingLoop();
    void decodeVideoPacket(AVPacket* packet);
    void decodeAudioPacket(AVPacket* packet);

    AVFormatContext* formatContext;
    AVCodecContext* videoCodecContext;
    AVCodecContext* audioCodecContext;
    SwsContext* swsContext;
    SwrContext* swrContext;
    AVFrame* frame;
    AVFrame* videoFrame;
    AVFrame* audioFrame;
    AVPacket* packet;
    
    int videoStreamIndex;
    int audioStreamIndex;
    
    std::atomic<bool> isPlaying;
    std::atomic<bool> isPaused;
    std::atomic<bool> shouldStop;
    std::atomic<bool> running;
    std::atomic<double> currentTime;
    std::atomic<double> duration;
    
    std::atomic<PlayerState> playerState;
    std::atomic<PlayerError> playerError;
    std::string errorMessage;
    
    std::thread decodingThread;
    std::thread readThread;
    std::mutex decodingMutex;
    std::condition_variable decodingCondition;
    std::mutex codecMutex;  // Protects codec contexts
    
    std::queue<AVPacket*> packetQueue;
    std::mutex queueMutex;
    std::condition_variable queueCond;
    
    uint8_t* rgbBuffer;
    
    VideoFrameCallback videoCallback;
    AudioFrameCallback audioCallback;
    
    std::map<double, VideoFrame> cachedVideoFrames;
    std::map<double, AudioFrame> cachedAudioFrames;
    std::mutex cacheMutex;
    
    int videoWidth;
    int videoHeight;
    double frameRate;
};
