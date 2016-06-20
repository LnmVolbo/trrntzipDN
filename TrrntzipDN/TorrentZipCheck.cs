﻿using System;
using System.Collections.Generic;

/***************************************************************************************************************

    Rules to be checked in a torrentzip file

    1)  Directory separator should be a '/'   
        a '\' is invalid and should be replaced with '/'
    
    2)  All Files in a torrentzip should be sorted with a lowercase file compare.

    3)  Directory Marker files are only needed if they are empty directories.

    4)  Also check for repeating files. (This is just a bad thing, which should never happen in a zip file)

***************************************************************************************************************/


namespace TrrntzipDN
{
    public static class TorrentZipCheck
    {
        public static TrrntZipStatus CheckZipFiles(ref List<ZippedFile> zippedFiles)
        {
            TrrntZipStatus tzStatus=TrrntZipStatus.Unknown;


            // ***************************** RULE 1 *************************************
            // Directory separator should be a '/' a '\' is invalid and should be replaced with '/'
            //
            // check if any '\' = 92 need converted to '/' = 47
            // this needs done before the sort, so that the sort is correct.
            // return BadDirectorySeparator if errors found.
            bool error1 = false;
            foreach (ZippedFile t in zippedFiles)
            {
                char[] bytes = t.Name.ToCharArray();
                bool fixDir = false;
                for (int j = 0; j < bytes.Length; j++)
                {
                    if (bytes[j] != 92) continue;
                    fixDir = true;
                    bytes[j] = (char)47;
                    tzStatus |= TrrntZipStatus.BadDirectorySeparator;
                    if (!error1 && Program.VerboseLogging)
                    {
                        error1 = true;
                        Console.WriteLine("Incorrect directory separator found");
                    }
                }
                if (fixDir) t.Name = new string(bytes);
            }


            // ***************************** RULE 2 *************************************
            // All Files in a torrentzip should be sorted with a lower case file compare.
            //
            // if needed sort the files correctly, and return Unsorted if errors found.
            bool error2 = false;
            bool thisSortFound = true;
            while (thisSortFound)
            {
                thisSortFound = false;
                for (int i = 0; i < zippedFiles.Count - 1; i++)
                {
                    int c = TrrntZipStringCompare(zippedFiles[i].Name, zippedFiles[i + 1].Name);
                    if (c > 0)
                    {
                        ZippedFile T = zippedFiles[i];
                        zippedFiles[i] = zippedFiles[i + 1];
                        zippedFiles[i + 1] = T;

                        tzStatus |= TrrntZipStatus.Unsorted;
                        thisSortFound = true;
                        if (!error2 && Program.VerboseLogging)
                        {
                            error2 = true;
                            Console.WriteLine("Incorrect file order found");
                        }

                    }
                }
            }


            // ***************************** RULE 3 *************************************
            // Directory marker files are only needed if they are empty directories.
            //
            // now that the files are sorted correctly, we can see if there are unneeded
            // directory files, by first finding directory files (these end in a '\' character ascii 92)
            // and then checking if the next file is a file in that found directory.
            // If we find this 2 entry pattern (directory followed by file in that directory)
            // then the directory entry should not be present and the torrentzip is incorrect.
            // return ExtraDirectoryEnteries if error is found. 
            bool error3 = false;
            for (int i = 0; i < zippedFiles.Count - 1; i++)
            {
                // check if this is a directory entry
                if (zippedFiles[i].Name[zippedFiles[i].Name.Length - 1] != 47)
                    continue;

                // check if the next filename is shorter or equal to this filename.
                // if it is shorter or equal it cannot be a file in the directory.
                if (zippedFiles[i + 1].Name.Length <= zippedFiles[i].Name.Length)
                    continue;

                // check if the directory part of the two file enteries match
                // if they do we found an incorrect directory entry.
                bool delete = true;
                for (int j = 0; j < zippedFiles[i].Name.Length; j++)
                {
                    if (zippedFiles[i].Name[j] != zippedFiles[i + 1].Name[j])
                    {
                        delete = false;
                        break;
                    }
                }

                // we found an incorrect directory so remove it.
                if (delete)
                {
                    zippedFiles.RemoveAt(i);
                    tzStatus |= TrrntZipStatus.ExtraDirectoryEnteries;
                    if (!error3 && Program.VerboseLogging)
                    {
                        error3 = true;
                        Console.WriteLine("Un-needed directory records found");
                    }

                    i--;
                }
            }


            // check for repeat files
            bool error4 = false;
            for (int i = 0; i < zippedFiles.Count - 1; i++)
            {
                if (zippedFiles[i].Name == zippedFiles[i + 1].Name)
                {
                    tzStatus |= TrrntZipStatus.RepeatFilesFound;
                    if (!error4 && Program.VerboseLogging)
                    {
                        error4 = true;
                        Console.WriteLine("Duplcate file enteries found");
                    }
                }
            }


            // if nothing was found wrong with the zip set it to valid trrntzip file
            if (tzStatus == TrrntZipStatus.Unknown)
                tzStatus = TrrntZipStatus.ValidTrrntzip;

            return tzStatus;
        }



        // perform an ascii based lower case string file compare
        private static int TrrntZipStringCompare(string string1, string string2)
        {
            char[] bytes1 = string1.ToCharArray();
            char[] bytes2 = string2.ToCharArray();

            int pos1 = 0;
            int pos2 = 0;

            for (;;)
            {
                if (pos1 == bytes1.Length)
                    return ((pos2 == bytes2.Length) ? 0 : -1);
                if (pos2 == bytes2.Length)
                    return 1;

                int byte1 = bytes1[pos1++];
                int byte2 = bytes2[pos2++];

                if (byte1 >= 65 && byte1 <= 90) byte1 += 0x20;
                if (byte2 >= 65 && byte2 <= 90) byte2 += 0x20;

                if (byte1 < byte2)
                    return -1;
                if (byte1 > byte2)
                    return 1;
            }
        }
    }
}
