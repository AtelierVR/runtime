-- client.lua — Lua TCP client example using the "tcp" module
-- This demonstrates the same event emitter pattern (on/off/once/emit) in Lua
-- Requires: local tcp = require('tcp')

local tcp = require('tcp')
local connect = tcp.connect
local socket = nil
local history = ""

function connectWithRetry()
    while true do
        print("[TCP Client] Attempting to connect to localhost:8080...")
        socket = connect("localhost", 8080)
        
        if not socket or not socket.connected then
            print("[TCP Client] Failed to connect, retrying in 15 seconds...")
            os.execute("sleep 15") -- 15 second delay
        else
            print("[TCP Client] Connected to localhost:8080")
            
            -- Subscribe to events
            socket.on("data", function(data)
                -- data is a byte[] (Lua string)
                local text = string.char(table.unpack(data)) -- convert bytes to string
                history = history .. text
                -- Keep only last 64 characters
                if #history > 64 then
                    history = string.sub(history, -64)
                end
                -- Update TMPro (exports.result would be set in Unity)
                if exports and exports.result then
                    exports.result.text = history
                end
                print("[TCP Client] Received: " .. history)
            end)
            
            socket.on("error", function(message)
                print("[TCP Client] Error: " .. message)
            end)
            
            socket.on("closed", function()
                print("[TCP Client] Connection closed")
            end)
            
            -- Wait for connection to close
            while socket.connected do
                os.execute("sleep 0.1") -- small sleep to not block
            end
            
            print("[TCP Client] Disconnected, reconnecting in 15 seconds...")
            os.execute("sleep 15")
        end
    end
end

-- Cleanup on script destroy
function onDestroy()
    if socket then
        socket.close()
        socket = nil
    end
end

-- Start connection loop
connectWithRetry()