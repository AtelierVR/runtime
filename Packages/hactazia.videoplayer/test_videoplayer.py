#!/usr/bin/env python3
"""
Test script for hactazia_videoplayer.dll
"""

import ctypes
import os
import sys
from ctypes import c_char_p, c_double, c_int, c_void_p, c_bool, POINTER, Structure

# Define the path to the DLL
DLL_PATH = os.path.join(os.path.dirname(__file__), "Plugins", "Windows", "x86_64", "hactazia_videoplayer.dll")

# Check if DLL exists
if not os.path.exists(DLL_PATH):
    print(f"Error: DLL not found at {DLL_PATH}")
    sys.exit(1)

# Load the DLL
try:
    videoplayer_dll = ctypes.CDLL(DLL_PATH)
    print(f"Successfully loaded DLL from {DLL_PATH}")
except Exception as e:
    print(f"Error loading DLL: {e}")
    sys.exit(1)

# Define C structures
class VideoFrame(Structure):
    _fields_ = [
        ("data", POINTER(ctypes.c_ubyte)),
        ("width", c_int),
        ("height", c_int),
        ("timestamp", c_double)
    ]

class AudioFrame(Structure):
    _fields_ = [
        ("data", POINTER(ctypes.c_ubyte)),
        ("size", c_int),
        ("timestamp", c_double)
    ]

# Define function prototypes
videoplayer_dll.CreateVideoPlayer.argtypes = []
videoplayer_dll.CreateVideoPlayer.restype = c_void_p

videoplayer_dll.DestroyVideoPlayer.argtypes = [c_void_p]
videoplayer_dll.DestroyVideoPlayer.restype = None

videoplayer_dll.LoadVideo.argtypes = [c_void_p, c_char_p]
videoplayer_dll.LoadVideo.restype = c_bool

videoplayer_dll.Play.argtypes = [c_void_p]
videoplayer_dll.Play.restype = None

videoplayer_dll.Pause.argtypes = [c_void_p]
videoplayer_dll.Pause.restype = None

videoplayer_dll.Resume.argtypes = [c_void_p]
videoplayer_dll.Resume.restype = None

videoplayer_dll.Stop.argtypes = [c_void_p]
videoplayer_dll.Stop.restype = None

videoplayer_dll.Seek.argtypes = [c_void_p, c_double]
videoplayer_dll.Seek.restype = None

videoplayer_dll.GetDuration.argtypes = [c_void_p]
videoplayer_dll.GetDuration.restype = c_double

videoplayer_dll.GetCurrentTime.argtypes = [c_void_p]
videoplayer_dll.GetCurrentTime.restype = c_double

videoplayer_dll.GetVideoWidth.argtypes = [c_void_p]
videoplayer_dll.GetVideoWidth.restype = c_int

videoplayer_dll.GetVideoHeight.argtypes = [c_void_p]
videoplayer_dll.GetVideoHeight.restype = c_int

videoplayer_dll.GetFrameRate.argtypes = [c_void_p]
videoplayer_dll.GetFrameRate.restype = c_double

videoplayer_dll.GetPlayerState.argtypes = [c_void_p]
videoplayer_dll.GetPlayerState.restype = c_int

videoplayer_dll.GetPlayerError.argtypes = [c_void_p]
videoplayer_dll.GetPlayerError.restype = c_int

videoplayer_dll.GetPlayerErrorMessage.argtypes = [c_void_p]
videoplayer_dll.GetPlayerErrorMessage.restype = c_char_p

videoplayer_dll.UpdatePlayer.argtypes = [c_void_p]
videoplayer_dll.UpdatePlayer.restype = None

test_url = b"https://rr2---sn-25glenlr.googlevideo.com/videoplayback?expire=1757627672&ei=t_DCaNi3O_XRp-oPrbzwuAs&ip=2a01%3Ae0a%3A86%3A2960%3A4561%3Accd2%3A589f%3A6fef&id=o-AHrKWqNoS9RjRyCvY_sPKBHwjNH4qP-35HJ3IKbxR_2I&itag=18&source=youtube&requiressl=yes&xpc=EgVo2aDSNQ%3D%3D&met=1757606071%2C&mh=0v&mm=31%2C26&mn=sn-25glenlr%2Csn-4g5ednsd&ms=au%2Conr&mv=m&mvi=2&pl=42&rms=au%2Cau&initcwndbps=2816250&bui=AY1jyLPsiTCgQ72DFiVMgoXt8A3OIad3FbikUuVWXYLozU_OFres7rAFbbaRucsMAUCpZ3B1KEQnXiqp&spc=l3OVKbGnPhD6v14Wlg9O&vprv=1&svpuc=1&mime=video%2Fmp4&ns=BptmMe2foTj6_NOnJS6W2IAQ&rqh=1&gir=yes&clen=9708078&ratebypass=yes&dur=146.796&lmt=1751204312557972&mt=1757605816&fvip=5&fexp=51552689%2C51565115%2C51565682%2C51580968&c=TVHTML5_SIMPLY&sefc=1&txp=5538534&n=xrJxgDFR5pGtGA&sparams=expire%2Cei%2Cip%2Cid%2Citag%2Csource%2Crequiressl%2Cxpc%2Cbui%2Cspc%2Cvprv%2Csvpuc%2Cmime%2Cns%2Crqh%2Cgir%2Cclen%2Cratebypass%2Cdur%2Clmt&lsparams=met%2Cmh%2Cmm%2Cmn%2Cms%2Cmv%2Cmvi%2Cpl%2Crms%2Cinitcwndbps&lsig=APaTxxMwRQIhAJsQ-yLREJPbCpF47IML_Ls204YcZLNJFIWzIX9j7bIHAiBKWdLBk9dsj0pQ9jtYA5JRfvFU0RZn-FWEEkyoPhT_CA%3D%3D&sig=AJfQdSswRgIhAOuXeXQQSPMvfTnyICn8LUMHuBCzk0jRN3yfNtvEb9N0AiEAwiH7BqNDaeP6nisETWpQyIykIJPAI6fiDBGYBpXZ0ow%3D"
    
def test_basic_functionality():
    """Test basic VideoPlayer functionality"""
    print("\n=== Testing Basic Functionality ===")
    
    # Create VideoPlayer
    print("Creating VideoPlayer...")
    player = videoplayer_dll.CreateVideoPlayer()
    if not player:
        print("ERROR: Failed to create VideoPlayer")
        return False
    print("✓ VideoPlayer created successfully")
    
    # Test initial state
    state = videoplayer_dll.GetPlayerState(player)
    error = videoplayer_dll.GetPlayerError(player)
    print(f"Initial state: {state}, error: {error}")
    
    # Test loading a video (this will likely fail without a valid video file)
    print("Testing LoadVideo with dummy URL...")
    result = videoplayer_dll.LoadVideo(player, test_url)
    print(f"LoadVideo result: {result}")
    
    # Test getting video properties
    print("Testing video properties...")
    duration = videoplayer_dll.GetDuration(player)
    current_time = videoplayer_dll.GetCurrentTime(player)
    width = videoplayer_dll.GetVideoWidth(player)
    height = videoplayer_dll.GetVideoHeight(player)
    frame_rate = videoplayer_dll.GetFrameRate(player)
    
    print(f"Duration: {duration}")
    print(f"Current time: {current_time}")
    print(f"Video dimensions: {width}x{height}")
    print(f"Frame rate: {frame_rate}")
    
    # Test player controls
    print("Testing player controls...")
    videoplayer_dll.Play(player)
    videoplayer_dll.UpdatePlayer(player)
    
    # Test playing for 10 seconds
    import time
    print("Playing video for 10 seconds...")
    start_time = time.time()
    while time.time() - start_time < 10.0:
        videoplayer_dll.UpdatePlayer(player)
        current_time = videoplayer_dll.GetCurrentTime(player)
        state = videoplayer_dll.GetPlayerState(player)
        print(f"Playing... Current time: {current_time:.2f}s, State: {state}", end='\r')
        time.sleep(0.1)  # Update every 100ms
    print()  # New line after the loop
    
    videoplayer_dll.Pause(player)
    videoplayer_dll.UpdatePlayer(player)
    print("Video paused after 10 seconds")
    
    videoplayer_dll.Resume(player)
    videoplayer_dll.UpdatePlayer(player)
    print("Video resumed")
    
    videoplayer_dll.Seek(player, 5.0)
    videoplayer_dll.UpdatePlayer(player)
    print("Seeked to 5.0 seconds")
    
    videoplayer_dll.Stop(player)
    videoplayer_dll.UpdatePlayer(player)
    print("Video stopped")
    
    # Check final state
    final_state = videoplayer_dll.GetPlayerState(player)
    final_error = videoplayer_dll.GetPlayerError(player)
    error_message = videoplayer_dll.GetPlayerErrorMessage(player)
    
    print(f"Final state: {final_state}, error: {final_error}")
    if error_message:
        print(f"Error message: {error_message.decode('utf-8')}")
    
    # Destroy VideoPlayer
    print("Destroying VideoPlayer...")
    videoplayer_dll.DestroyVideoPlayer(player)
    print("✓ VideoPlayer destroyed")
    
    return True

def test_memory_management():
    """Test memory management by creating and destroying multiple players"""
    print("\n=== Testing Memory Management ===")
    
    players = []
    num_players = 10
    
    # Create multiple players
    print(f"Creating {num_players} VideoPlayers...")
    for i in range(num_players):
        player = videoplayer_dll.CreateVideoPlayer()
        if player:
            players.append(player)
        else:
            print(f"ERROR: Failed to create player {i}")
            break
    
    print(f"✓ Created {len(players)} VideoPlayers")
    
    # Destroy all players
    print("Destroying all VideoPlayers...")
    for i, player in enumerate(players):
        videoplayer_dll.DestroyVideoPlayer(player)
    
    print("✓ All VideoPlayers destroyed")
    return True

def test_error_handling():
    """Test error handling with invalid parameters"""
    print("\n=== Testing Error Handling ===")
    
    # Test with null player
    print("Testing functions with null player...")
    
    # These should not crash
    videoplayer_dll.DestroyVideoPlayer(None)
    result = videoplayer_dll.LoadVideo(None, test_url)
    print(f"LoadVideo with null player: {result}")
    
    videoplayer_dll.Play(None)
    videoplayer_dll.Pause(None)
    videoplayer_dll.Resume(None)
    videoplayer_dll.Stop(None)
    videoplayer_dll.Seek(None, 0.0)
    videoplayer_dll.UpdatePlayer(None)
    
    duration = videoplayer_dll.GetDuration(None)
    current_time = videoplayer_dll.GetCurrentTime(None)
    width = videoplayer_dll.GetVideoWidth(None)
    height = videoplayer_dll.GetVideoHeight(None)
    frame_rate = videoplayer_dll.GetFrameRate(None)
    state = videoplayer_dll.GetPlayerState(None)
    error = videoplayer_dll.GetPlayerError(None)
    
    print(f"Properties with null player - Duration: {duration}, Time: {current_time}")
    print(f"Dimensions: {width}x{height}, FPS: {frame_rate}, State: {state}, Error: {error}")
    
    print("✓ Error handling tests completed")
    return True

def main():
    """Main test function"""
    print("Starting hactazia_videoplayer.dll tests...")
    
    try:
        # Run all tests
        success = True
        success &= test_basic_functionality()
        success &= test_memory_management()
        success &= test_error_handling()
        
        if success:
            print("\n🎉 All tests completed successfully!")
        else:
            print("\n❌ Some tests failed!")
            
    except Exception as e:
        print(f"\n💥 Test execution failed: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    main()