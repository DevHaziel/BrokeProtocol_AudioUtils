using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrokeProtocol.API;
using BrokeProtocol.Client.Opus;
using BrokeProtocol.Entities;
using BrokeProtocol.Utility.Networking;
using UnityEngine;

namespace AudioUtils
{
    public class Core : Plugin
    {
        public Core()
        {
            Info = new PluginInfo("AudioUtils", "audio");
            if (!Directory.Exists(Path.Combine("Plugins", "Music"))) Directory.CreateDirectory(Path.Combine("Plugins", "Music"));
        }
    }
    public class Commands : IScript
    {
        public Commands() { 
            CommandHandler.RegisterCommand("playAudio", new Action<ShPlayer, string>(PlayAudio));
        }

        public void PlayAudio(ShPlayer player, string fileName)
        {
            string audioFile = Path.Combine("Plugins", "Music", fileName);
            if(!File.Exists(audioFile))
            {
                player.svPlayer.SendGameMessage("Audio file not found!");
                return;
            }
            int sampleSize = 960;
            player.StartCoroutine(SendOpusChunks(player, audioFile, sampleSize));
        }
        /** 
        Send a single tone for X seconds to test the system 
        */
        private IEnumerator SendToneTest(ShPlayer player, float frequency, int durationSeconds, int sampleRate = 16000)
        {
            int totalSamples = sampleRate * durationSeconds;
            float[] samples = new float[960];
            var encoder = new OpusEncoder(sampleRate);

            for (int i = 0; i < totalSamples; i += 960)
            {
                for (int j = 0; j < 960; j++)
                {
                    float time = (i + j) / (float)sampleRate;
                    samples[j] = Mathf.Sin(2 * Mathf.PI * frequency * time); // Generate a pure tone
                }

                // Opus encoder
                byte[] encodedBuffer = new byte[400];
                int encodedLength = encoder.EncodeFloat(samples, ref encodedBuffer);

                if (encodedLength > 0)
                {
                    byte[] finalChunk = new byte[encodedLength];
                    Buffer.BlockCopy(encodedBuffer, 0, finalChunk, 0, encodedLength);

                    // Send to client
                    player.svPlayer.Send(SvSendType.Self, Channel.Unreliable, ClPacket.ChatVoice, player.ID, finalChunk);
                }

                yield return new WaitForSeconds(0.06f); // 60 ms between fragments
            }
        }
        private IEnumerator SendOpusChunks(ShPlayer player, string audioFile, int sampleSize)
        {
            if (!File.Exists(audioFile))
            {
                player.svPlayer.SendGameMessage("Audio file not found!");
                yield break;
            }

            byte[] pcmData = File.ReadAllBytes(audioFile);
            int totalSamples = pcmData.Length / sizeof(float);
            var encoder = new OpusEncoder(16000);

            int totalChunks = Mathf.CeilToInt((float)totalSamples / sampleSize);
            float sampleDuration = (float)sampleSize/16000f;

            for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                int startSample = chunkIndex * sampleSize;
                int currentChunkSize = Math.Min(sampleSize, totalSamples - startSample);
                float[] sampleBuffer = new float[sampleSize];
                Buffer.BlockCopy(pcmData, startSample * sizeof(float), sampleBuffer, 0, currentChunkSize * sizeof(float));

                if (currentChunkSize < sampleSize)
                {
                    Array.Clear(sampleBuffer, currentChunkSize, sampleSize - currentChunkSize);
                }

                byte[] encodedBuffer = new byte[400];
                int encodedLength = encoder.EncodeFloat(sampleBuffer, ref encodedBuffer);

                if (encodedLength > 0)
                {
                    byte[] finalChunk = new byte[encodedLength];
                    Buffer.BlockCopy(encodedBuffer, 0, finalChunk, 0, encodedLength);

                    player.svPlayer.Send(SvSendType.Self, Channel.Reliable, ClPacket.ChatVoiceCall, player.ID, finalChunk);
                } else
                {
                    Debug.LogWarning($"Error encoding the fragment: {chunkIndex + 1}");
                }
                yield return new WaitForSeconds(sampleDuration);
            }
            player.svPlayer.SendGameMessage("Audio play finished.");
        }
    }
    public class OpusReader
    {
        public static byte[] ReadOpusFile(string path)
        {
            if(!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }

            return File.ReadAllBytes(path);
        }
        public static IEnumerable<byte[]> ReadOpusFileInChunks(string path, int chunkSize)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
            byte[] fileData = File.ReadAllBytes(path);

            for(int i = 0; i < fileData.Length; i += chunkSize)
            {
                yield return fileData.Skip(i).Take(chunkSize).ToArray();
            }
        }
    }
}
