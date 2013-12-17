﻿using System;
using System.Collections.Generic;
using System.IO;
using libZPlay;

namespace TITS
{
    class Program
    {
		[STAThread]
        static void Main(string[] args)
        {
            Console.Title = "TITS";
            Console2.WriteLine(ConsoleColor.White, "TITS Console");

			TITS.Components.NowPlaying PleeTits = new Components.NowPlaying();
            try
            {
                if (args != null && args.Length > 0)
                    PleeTits.Playlist = LoadMultiple(args);
                else
                    PleeTits.Playlist = LoadMultiple(Interaction.BrowseFiles(isFolderPicker: true));

                PleeTits.SongChanged += (sender, e) => { Interaction.PrintSong(e.Song); };
				PleeTits.StartPlaying();

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        while (Console.KeyAvailable)
                        {
                            key = Console.ReadKey(true);
                        }

                        // Stopping playback has to be outside of the switch for break to work
                        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.MediaStop)
                        {
                            PleeTits.Stop();
                            break;
                        }

                        switch (key.Key)
                        {
                            case ConsoleKey.RightArrow:
                            case ConsoleKey.MediaNext:
                                PleeTits.Next();
                                break;
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Console2.WriteLine(ConsoleColor.Red, ex.ToString());
            }
        }

        static Library.Playlist LoadMultiple(IEnumerable<string> items)
        {
            Library.Playlist playlist = new Library.Playlist();
            if (items != null)
            {
                foreach (string item in items)
                {
                    playlist.Add(item);
                }
            }
            return playlist;
        }

        static void StartLoop(string filename)
        {
            ZPlay player = new ZPlay();

            Console.WriteLine("Opening {0}...", filename);
            player.SetSettings(TSettingID.sidAccurateLength, 1);
            player.SetSettings(TSettingID.sidAccurateSeek, 1);

            if (System.IO.File.Exists(filename.Replace("_loop", "_intro")))
                player.OpenFile(filename.Replace("_loop", "_intro"), TStreamFormat.sfAutodetect);
            else
                player.OpenFile(filename, TStreamFormat.sfAutodetect);

            if (!player.StartPlayback())
            {
                throw new Exception(player.GetError());
            }

            // ID3v2
            TID3Info info = default(TID3Info);
            if (player.LoadID3(TID3Version.id3Version2, ref info) && info.Title.Length > 0)
            {
                Console2.Write(ConsoleColor.Magenta, info.Title);
                Console2.Write(ConsoleColor.DarkMagenta, " - ");
                Console2.Write(ConsoleColor.Magenta, info.Artist);
                Console2.Write(ConsoleColor.DarkMagenta, " (");
                Console2.Write(ConsoleColor.Magenta, info.Album);
                Console2.WriteLine(ConsoleColor.DarkMagenta, ")");
            }

            // Get track length
            TStreamInfo streamInfo = default(TStreamInfo);
            player.GetStreamInfo(ref streamInfo);
            TStreamTime totalTime = streamInfo.Length;

            // Playback loop
            int playcount = 0;
            TStreamStatus status = default(TStreamStatus);
            player.GetStatus(ref status);
            while (status.fPlay)
            {
                player.GetStreamInfo(ref streamInfo);
                totalTime = streamInfo.Length;

                // Escape?
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        Console2.WriteLine(ConsoleColor.DarkRed, "[Stopped] ");
                        player.Close();
                        break;
                    }
                }

                // Enqueue for gapless playback
                if (status.nSongsInQueue == 0)
                {
                    playcount++;
                    player.AddFile(filename, TStreamFormat.sfAutodetect);
                }

                // Display running time
                TStreamTime time = default(TStreamTime);
                player.GetPosition(ref time);

                Console2.Write(ConsoleColor.DarkGreen, "[Playing] ");
                Console2.Write(ConsoleColor.Green, "{0:0}:{1:00} / {2:0}:{3:00} ", time.hms.minute, time.hms.second, totalTime.hms.minute, totalTime.hms.second);
                Console2.Write(ConsoleColor.DarkGreen, "({0})\r", playcount);

                System.Threading.Thread.Sleep(10);
                player.GetStatus(ref status);
            }
        }
    }
}
