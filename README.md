# AudioUtils

Send audio data to the player using the game voicechat system

The audio file / data require those things:
Sample rate = 16000
Audio channels = 1
File format = f32le (32 bits floats)

You can convert any audio file to the required format using ffmpeg with this command:

ffmpeg -i input.mp3 -ar 16000 -ac 1 -f f32le output.raw
