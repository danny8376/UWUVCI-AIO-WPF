﻿using GameBaseClassLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Xml;
using UWUVCI_AIO_WPF.Classes;
using UWUVCI_AIO_WPF.Properties;
using UWUVCI_AIO_WPF.UI.Windows;

namespace UWUVCI_AIO_WPF
{
    internal static class Injection
    {
      

        private static readonly string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "temp");
        private static readonly string baseRomPath = Path.Combine(tempPath, "baserom");
        private static readonly string imgPath = Path.Combine(tempPath, "img");
        private static readonly string toolsPath = Path.Combine(Directory.GetCurrentDirectory(), "Tools");
        static string code = null;

        /*
         * GameConsole: Can either be NDS, N64, GBA, NES, SNES or TG16
         * baseRom = Name of the BaseRom, which is the folder name too (example: Super Metroid EU will be saved at the BaseRom path under the folder SMetroidEU, so the BaseRom is in this case SMetroidEU).
         * customBasePath = Path to the custom Base. Is null if no custom base is used.
         * injectRomPath = Path to the Rom to be injected into the Base Game.
         * bootImages = String array containing the paths for
         *              bootTvTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 1280x720 and have a bit depth of 24. If null, the original BootImage will be used.
         *              bootDrcTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 854x480 and have a bit depth of 24. If null, the original BootImage will be used.
         *              iconTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 128x128 and have a bit depth of 32. If null, the original BootImage will be used.
         *              bootLogoTex: PNG or TGA (PNG gets converted to TGA using UPNG). Needs to be in the dimensions 170x42 and have a bit depth of 32. If null, the original BootImage will be used.
         * gameName = The name of the final game to be entered into the .xml files.
         * iniPath = Only used for N64. Path to the INI configuration. If "blank", a blank ini will be used.
         * darkRemoval = Only used for N64. Indicates whether the dark filter should be removed.
         */

        public static bool Inject(GameConfig Configuration, string RomPath, MainViewModel mvm, bool force)
        {
            if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            Directory.CreateDirectory(tempPath);
            mvm.InjcttoolCheck();
            try
            {

                if (Configuration.BaseRom.Name != "Custom")
                {
                    //Normal Base functionality here
                    CopyBase($"{Configuration.BaseRom.Name.Replace(":", "")} [{Configuration.BaseRom.Region.ToString()}]", null);
                }
                else
                {
                    //Custom Base Functionality here
                    CopyBase($"Custom", Configuration.CBasePath);
                }
                if (mvm.GC)
                {
                    RunSpecificInjection(Configuration, GameConsoles.GCN, RomPath, force, mvm);
                }
                else
                {
                    RunSpecificInjection(Configuration, Configuration.Console, RomPath, force, mvm);
                }
               
                EditXML(Configuration.GameName, mvm.Index, code);
                Images(Configuration);
                MessageBox.Show("Injection Finished, please choose how you want to export the Inject next", "Finished Injection Part", MessageBoxButton.OK, MessageBoxImage.Information);
                code = null;
                return true;
            }catch(Exception e)
            {
                code = null;
                if (e.Message.Contains("Images")){
                    MessageBox.Show("Injection Failed due to wrong BitDepth, please check if your Files are in a different bitdepth than 32bit or 24bit", "Injection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (e.Message.Contains("Size"))
                {
                    MessageBox.Show("Injection Failed due to Image Issues. Please check if your Images are made using following Information:\n\niconTex: \nDimensions: 128x128\nBitDepth: 32\n\nbootDrcTex: \nDimensions: 854x480\nBitDepth: 24\n\nbootTvTex: \nDimensions: 1280x720\nBitDepth: 24\n\nbootLogoTex: \nDimensions: 170x42\nBitDepth: 32", "Injection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (e.Message.Contains("retro"))
                {
                    MessageBox.Show("The ROM you want to Inject is to big for selected Base!\nPlease try again with different Base", "Injection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                else
                {
                    MessageBox.Show("Injection Failed due to unknown circumstances, please contact us on the UWUVCI discord", "Injection Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                }
                Clean();
                return false;
            }
            finally
            {
                mvm.Index = -1;
                mvm.LR = false;
            }

        }

        private static void RunSpecificInjection(GameConfig cfg, GameConsoles console, string RomPath, bool force, MainViewModel mvm)
        {
            switch (console)
            {
                case GameConsoles.NDS:
                    NDS(RomPath);
                    break;

                case GameConsoles.N64:
                   N64(RomPath, cfg.N64Stuff);
                    break;

                case GameConsoles.GBA:
                    GBA(RomPath);
                    break;

                case GameConsoles.NES:
                    NESSNES(RomPath);
                    break;
                case GameConsoles.SNES:
                    NESSNES(RemoveHeader(RomPath));
                    break;
                case GameConsoles.TG16:
                    TG16(RomPath);
                    break;
                case GameConsoles.MSX:
                    MSX(RomPath);
                    break;
                case GameConsoles.WII:
                    WII(RomPath, mvm, false);
                    break;
                case GameConsoles.GCN:
                    WII(RomPath, mvm, force);
                    break;
            }
        }

        private static void WII(string romPath, MainViewModel mvm, bool force)
        {
            Console.WriteLine("Removing unnecessary Files...");

            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "content"), "*.nfs"))
            {
                File.Delete(sFile);
            }
            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "meta"), "*.jpg"))
            {
                File.Delete(sFile);
            }
            foreach (string sFile in Directory.GetFiles(Path.Combine(baseRomPath, "meta"), "*.bfma"))
            {
                File.Delete(sFile);
            }

            if (File.Exists(Path.Combine(baseRomPath, "code", "rvlt.tmd"))) File.Delete(Path.Combine(baseRomPath, "code", "rvlt.tmd"));
            if (File.Exists(Path.Combine(baseRomPath, "code", "rvlt.tik"))) File.Delete(Path.Combine(baseRomPath, "code", "rvlt.tik"));

            Console.WriteLine("Finished removing Files");

            using (Process tik = new Process())
            {
                if (!mvm.GC)
                {
                    if (new FileInfo(romPath).Extension.Contains("wbfs"))
                    {
                        Console.WriteLine("Converting WBFS to ISO...");
                        tik.StartInfo.FileName = Path.Combine(toolsPath, "wbfs_file.exe");
                        tik.StartInfo.Arguments = $"\"{romPath}\" convert \"{Path.Combine(tempPath, "pre.iso")}\"";
                        tik.Start();
                        tik.WaitForExit();
                        if (!File.Exists(Path.Combine(tempPath, "pre.iso")))
                        {
                            Console.WriteLine("An error occured while converting WBFS to ISO");
                            throw new Exception();
                        }
                        if (File.Exists(Path.Combine(tempPath, "rom.wbfs"))) { File.Delete(Path.Combine(tempPath, "rom.wbfs")); }
                        romPath = Path.Combine(tempPath, "pre.iso");
                        Console.WriteLine("Finished Conversion");
                    }
                    tik.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");

                    Console.WriteLine("Trimming ROM...");

                    tik.StartInfo.Arguments = $"extract \"{romPath}\" --DEST \"{Path.Combine(tempPath, "IsoExt")}\" --psel data -vv1";
                    tik.Start();
                    tik.WaitForExit();
                    if (!Directory.Exists(Path.Combine(tempPath, "IsoExt")))
                    {
                        Console.Clear();
                        Console.WriteLine("An error occured while trimming the ROM");
                        throw new Exception();
                    }
                    Console.WriteLine("Finished trimming");
                    if (mvm.Index == 4)
                    {
                        Console.WriteLine("Patching the ROM to force Classic Controller input");
                        tik.StartInfo.FileName = Path.Combine(toolsPath, "GetExtTypePatcher.exe");
                        tik.StartInfo.Arguments = $"\"{Path.Combine(tempPath, "IsoExt", "sys", "main.dol")}\"";
                        tik.Start();
                        tik.WaitForExit();
                        tik.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                    }
                    Console.WriteLine("Creating ISO from trimmed ROM...");
                    tik.StartInfo.Arguments = $"copy \"{Path.Combine(tempPath, "IsoExt")}\" --DEST \"{Path.Combine(tempPath, "game.iso")}\" -ovv --links --iso";
                    tik.Start();
                    tik.WaitForExit();

                    if (!File.Exists(Path.Combine(tempPath, "game.iso")))
                    {
                        Console.Clear();
                        Console.WriteLine("An error occured while Creating the ISO");
                        throw new Exception();
                    }
                    romPath = Path.Combine(tempPath, "game.iso");
                }
                else
                {
                    if (Directory.Exists(Path.Combine(tempPath, "TempBase"))) Directory.Delete(Path.Combine(tempPath, "TempBase"), true);
                    Directory.CreateDirectory(Path.Combine(tempPath, "TempBase"));
                    tik.StartInfo.FileName =  Path.Combine(toolsPath, "7za.exe");
                    tik.StartInfo.Arguments = $"x \"{Path.Combine(toolsPath, "BASE.zip")}\" -o\"{Path.Combine(tempPath)}\"";
                    tik.Start();
                    tik.WaitForExit();
                    DirectoryCopy(Path.Combine(tempPath, "BASE"), Path.Combine(tempPath, "TempBase"), true);
                    if (force)
                    {
                        File.Copy(Path.Combine(toolsPath, "nintendont_force.dol"), Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
                    }
                    else
                    {
                        File.Copy(Path.Combine(toolsPath, "nintendont.dol"), Path.Combine(tempPath, "TempBase", "sys", "main.dol"));
                    }
                    File.Copy(romPath, Path.Combine(tempPath, "TempBase", "files", "game.iso"));
                    tik.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                    tik.StartInfo.Arguments = $"copy \"{Path.Combine(tempPath, "TempBase")}\" --DEST \"{Path.Combine(tempPath, "game.iso")}\" -ovv --links --iso";
                    tik.Start();
                    tik.WaitForExit();
                    if (!File.Exists(Path.Combine(tempPath, "game.iso")))
                    {
                        Console.Clear();
                        Console.WriteLine("An error occured while Creating the ISO");
                        throw new Exception();
                    }
                    romPath = Path.Combine(tempPath, "game.iso");
                }

                Console.WriteLine("Extracting Ticket and TMD from ISO...");
                tik.StartInfo.FileName = Path.Combine(toolsPath, "wit.exe");
                tik.StartInfo.Arguments = $"extract \"{romPath}\" --psel data --files +tmd.bin --files +ticket.bin --dest \"{Path.Combine(tempPath, "tik")}\" -vv1";
                tik.Start();
                tik.WaitForExit();
                if (!Directory.Exists(Path.Combine(tempPath, "tik")) || !File.Exists(Path.Combine(tempPath, "tik", "tmd.bin")) || !File.Exists(Path.Combine(tempPath, "tik", "ticket.bin")))
                {
                    Console.Clear();
                    Console.WriteLine("An error occured while extracting the Ticket and TMD");
                    throw new Exception();
                }
                Console.WriteLine("Finished extracting");
                Console.WriteLine("Copying TIK and TMD...");
                if (File.Exists(Path.Combine(baseRomPath, "code", "rvlt.tmd"))) { File.Delete(Path.Combine(baseRomPath, "code", "rvlt.tmd")); }
                File.Copy(Path.Combine(tempPath, "tik", "tmd.bin"), Path.Combine(baseRomPath, "code", "rvlt.tmd"));
                if (File.Exists(Path.Combine(baseRomPath, "code", "rvlt.tik"))) { File.Delete(Path.Combine(baseRomPath, "code", "rvlt.tik")); }
                File.Copy(Path.Combine(tempPath, "tik", "ticket.bin"), Path.Combine(baseRomPath, "code", "rvlt.tik"));
                if (!File.Exists(Path.Combine(baseRomPath, "code", "rvlt.tik")) || !File.Exists(Path.Combine(baseRomPath, "code", "rvlt.tmd")))
                {
                    Console.Clear();
                    Console.WriteLine("An error occured while copying the Ticket and TMD");
                    throw new Exception();
                }
                Console.WriteLine("Finished Copying");
                Console.WriteLine("Converting Game to NFS format...");
                string olddir = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(Path.Combine(baseRomPath, "content"));
                tik.StartInfo.FileName = Path.Combine(toolsPath, "nfs2iso2nfs.exe");
                if (!mvm.GC)
                {
                    string extra = "";
                    if (mvm.Index == 2)
                    {
                        extra = "-horizontal ";
                    }
                    if (mvm.Index == 3) { extra = "-wiimote "; }
                    if (mvm.Index == 4) { extra = "-instantcc "; }
                    if (mvm.Index == 5) { extra = "-nocc "; }
                    if (mvm.LR) { extra += "-lrpatch "; }
                    Console.WriteLine(extra);
                    Console.ReadLine();
                    tik.StartInfo.Arguments = $"-enc {extra}-iso \"{romPath}\"";
                }
                else
                {
                    tik.StartInfo.Arguments = $"-enc -homebrew -passthrough -iso \"{romPath}\"";
                }
                tik.Start();
                tik.WaitForExit();
                Console.WriteLine("Finished Conversion");
                Directory.SetCurrentDirectory(olddir);
            }
        }

       
        public static void MSX(string injectRomPath)
        {
            byte[] test = new byte[0x580B3];
            using (var fs = new FileStream(Path.Combine(baseRomPath, "content" , "msx", "msx.pkg"),
                                 FileMode.Open,
                                 FileAccess.ReadWrite))
            {


                fs.Read(test, 0, 0x580B3);
                fs.Close();
                File.Delete(Path.Combine(baseRomPath, "content", "msx", "msx.pkg"));
            }
            using (var fs = new FileStream(Path.Combine(baseRomPath, "content", "msx", "msx.pkg"),
                                 FileMode.OpenOrCreate,
                                 FileAccess.ReadWrite))
            {


                fs.Write(test, 0, 0x580B3);
                fs.Close();

            }
            using (var fs = new FileStream(injectRomPath,
                                 FileMode.OpenOrCreate,
                                 FileAccess.ReadWrite))
            {


                test = new byte[fs.Length];
                fs.Read(test, 0, test.Length - 1);

            }
            using (var fs = new FileStream(Path.Combine(baseRomPath, "content", "msx", "msx.pkg"),
                                FileMode.Append))
            {

                fs.Write(test, 0, test.Length);

            }
        }
       
        public static void Clean()
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }

        public static void Loadiine(string gameName)
        {
            if (gameName == null || gameName == string.Empty) gameName = "NoName";
            //string outputPath = Path.Combine(Properties.Settings.Default.InjectionPath, gameName);
            string outputPath = Path.Combine(Properties.Settings.Default.OutPath, $"[LOADIINE]{gameName}");
            int i = 0;
            while (Directory.Exists(outputPath))
            {
                outputPath = Path.Combine(Properties.Settings.Default.OutPath, $"[LOADIINE]{gameName}_{i}");
                i++;
            }

            DirectoryCopy(baseRomPath,outputPath, true);
            MessageBox.Show($"Injection Complete! The Inject is stored here:\n{outputPath}\n\nThe Configuration will not be cleared, so you can Export the Config if you want. To clear the Configuration, reselect the Console you want to Inject into.", "Inject Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            Clean();
        }

        public static void Packing(string gameName, MainViewModel mvm)
        {
            mvm.InjcttoolCheck();
            if (gameName == null || gameName == string.Empty) gameName = "NoName";
            //string outputPath = Path.Combine(Properties.Settings.Default.InjectionPath, gameName);
            string outputPath = Path.Combine(Properties.Settings.Default.OutPath, $"[WUP]{gameName}");
            int i = 0;
            while (Directory.Exists(outputPath))
            {
                outputPath = Path.Combine(Properties.Settings.Default.OutPath, $"[WUP]{gameName}_{i}");
                i++;
            }

            using (Process cnuspacker = new Process())
            {
                cnuspacker.StartInfo.UseShellExecute = false;
                cnuspacker.StartInfo.CreateNoWindow = true;
                cnuspacker.StartInfo.FileName = Path.Combine(toolsPath, "CNUSPACKER.exe");
                cnuspacker.StartInfo.Arguments = $"-in \"{baseRomPath}\" -out \"{outputPath}\" -encryptKeyWith {Properties.Settings.Default.Ckey}";

                cnuspacker.Start();
                cnuspacker.WaitForExit();
            }
            string extra = "";
            if (mvm.GameConfiguration.Console == GameConsoles.WII) extra = "\nDISCLAIMER: Some games cannot reboot into the WiiU Menu. Shut down the console via the GamePad Power Button.\nIf Stuck in a BlackScreen, you need to unplug your wiiu";
            if (mvm.GC) extra = "\nDISCLAIMER: Make sure to have Nintendont + config on your sd card. You can add them under Settings -> \"Start Nintendont Config Tool\"";
            MessageBox.Show($"Injection Complete!\nDisclaimer: Only install injections to USB to prevent a brick in a worst case scenario{extra}\n\nThe Inject is stored here:\n{outputPath}\n\nThe Configuration will not be cleared, so you can Export the Config if you want. To clear the Configuration, reselect the Console you want to Inject into.", "Inject Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            Clean();
        }

        public static void Download(MainViewModel mvm)

        {
            mvm.InjcttoolCheck();
            GameBases b = mvm.getBasefromName(mvm.SelectedBaseAsString);

            //GetKeyOfBase
            TKeys key = mvm.getTkey(b);
            if (mvm.GameConfiguration.Console == GameConsoles.WII || mvm.GameConfiguration.Console == GameConsoles.GCN)
            {
                using (Process zip = new Process())
                {
                    if(Directory.Exists(Path.Combine(toolsPath, "IKVM"))) { Directory.Delete(Path.Combine(toolsPath, "IKVM"), true); }
                    zip.StartInfo.FileName = Path.Combine(toolsPath, "7za.exe");
                    zip.StartInfo.Arguments = $"x \"{Path.Combine(toolsPath, "IKVM.zip")}\" -o\"{Path.Combine(toolsPath, "IKVM")}\"";
                    zip.Start();
                    zip.WaitForExit();
                    string[] JNUSToolConfig = { "http://ccs.cdn.wup.shop.nintendo.net/ccs/download", Properties.Settings.Default.Ckey };
                    string savedir = Directory.GetCurrentDirectory();
                    
                    File.WriteAllLines(Path.Combine(toolsPath, "IKVM", "config"), JNUSToolConfig);
                    Directory.SetCurrentDirectory(Path.Combine(toolsPath, "IKVM"));
                    zip.StartInfo.FileName = "JNUSTool.exe";
                    zip.StartInfo.Arguments = $"{b.Tid} {key.Tkey} -file .*";
                    zip.Start();
                    zip.WaitForExit();
                    Directory.SetCurrentDirectory(savedir);
                    var directories = Directory.GetDirectories(Path.Combine(toolsPath, "IKVM"));
                    string name = "";
                    foreach (var s in directories)
                    {
                        if (s.Contains(b.Name))
                        {
                            var split = s.Split('\\');
                            name = split[split.Length - 1];

                        }

                    }
                    DirectoryCopy(Path.Combine(toolsPath, "IKVM", name), Path.Combine(Properties.Settings.Default.BasePath, $"{b.Name.Replace(":", "")} [{b.Region.ToString()}]"), true);
                    Directory.Delete(Path.Combine(toolsPath, "IKVM"), true);
                }
            }
            else
            {
              


                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
                Directory.CreateDirectory(tempPath);
                using (Process download = new Process())
                {
                    download.StartInfo.FileName = Path.Combine(toolsPath, "WiiUDownloader.exe");
                    download.StartInfo.Arguments = $"{b.Tid} {key.Tkey} \"{Path.Combine(tempPath, "download")}\"";

                    download.Start();
                    download.WaitForExit();
                }

                using (Process decrypt = new Process())
                {
                    decrypt.StartInfo.FileName = Path.Combine(toolsPath, "Cdecrypt.exe");
                    decrypt.StartInfo.Arguments = $"{Properties.Settings.Default.Ckey} \"{Path.Combine(tempPath, "download")}\" \"{Path.Combine(Properties.Settings.Default.BasePath, $"{b.Name.Replace(":", "")} [{b.Region.ToString()}]")}\"";

                    decrypt.Start();
                    decrypt.WaitForExit();
                }
            }
            //GetCurrentSelectedBase
            
        }
        public static string ExtractBase(string path, GameConsoles console)
        {
            if(!Directory.Exists(Path.Combine(Properties.Settings.Default.BasePath, "CustomBases")))
            {
                Directory.CreateDirectory(Path.Combine(Properties.Settings.Default.BasePath, "CustomBases"));
            }
            string outputPath = Path.Combine(Properties.Settings.Default.BasePath, "CustomBases", $"[{console.ToString()}] Custom");
            int i = 0;
            while (Directory.Exists(outputPath))
            {
                outputPath = Path.Combine(Properties.Settings.Default.BasePath, $"[{console.ToString()}] Custom_{i}");
                i++;
            }
            using (Process decrypt = new Process())
            {
                decrypt.StartInfo.FileName = Path.Combine(toolsPath, "Cdecrypt.exe");
                decrypt.StartInfo.Arguments = $"{Properties.Settings.Default.Ckey} \"{path}\" \"{outputPath}";

                decrypt.Start();
                decrypt.WaitForExit();
            }
            return outputPath;
        }
        // This function changes TitleID, ProductCode and GameName in app.xml (ID) and meta.xml (ID, ProductCode, Name)
        private static void EditXML(string gameName, int index, string code)
        {
            
            string metaXml = Path.Combine(baseRomPath, "meta", "meta.xml");
            string appXml = Path.Combine(baseRomPath, "code", "app.xml");
            Random random = new Random();
            string ID = $"{random.Next(0x3000, 0x10000):X4}";
            
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(metaXml);
                    if (gameName != null && gameName != string.Empty)
                    {
                        doc.SelectSingleNode("menu/longname_ja").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_en").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_fr").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_de").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_it").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_es").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_zhs").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_ko").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_nl").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_pt").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_ru").InnerText = gameName;
                        doc.SelectSingleNode("menu/longname_zht").InnerText = gameName;
                    }

                    if(code != null)
                    {
                        doc.SelectSingleNode("menu/product_code").InnerText = $"WUP-N-{code}";
                    }
                    else
                    {
                        doc.SelectSingleNode("menu/product_code").InnerText = $"WUP-N-{ID}";
                    }
                     if (index > 0)
                    {
                    doc.SelectSingleNode("menu/drc_use").InnerText = "65537";
                    }
                doc.SelectSingleNode("menu/title_id").InnerText = $"0005000060{ID}00";
                    doc.SelectSingleNode("menu/group_id").InnerText = $"0000{ID}";
                    if (gameName != null && gameName != string.Empty)
                    {
                        doc.SelectSingleNode("menu/shortname_ja").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_fr").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_de").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_en").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_it").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_es").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_zhs").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_ko").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_nl").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_pt").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_ru").InnerText = gameName;
                        doc.SelectSingleNode("menu/shortname_zht").InnerText = gameName;
                    }

                    doc.Save(metaXml);
                }
                catch (NullReferenceException)
                {
                    //MessageBox.Show("Error when editing the meta.xml: Values seem to be missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                try
                {
                    doc.Load(appXml);
                    doc.SelectSingleNode("app/title_id").InnerText = $"0005000060{ID}00";
                    doc.SelectSingleNode("app/group_id").InnerText = $"0000{ID}";
                    doc.Save(appXml);
                }
                catch (NullReferenceException)
                {
                    // MessageBox.Show("Error when editing the app.xml: Values seem to be missing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            
            
        }

        //This function copies the custom or normal Base to the working directory
        private static void CopyBase(string baserom, string customPath)
        {
            if (Directory.Exists(baseRomPath)) // sanity check
            {
                Directory.Delete(baseRomPath, true);
            }
            if (baserom == "Custom")
            {
                DirectoryCopy(customPath, baseRomPath, true);
            }
            else
            {
                DirectoryCopy(Path.Combine(Properties.Settings.Default.BasePath, baserom), baseRomPath, true);
            }
        }

        private static void TG16(string injectRomPath)
        {
            //checking if folder
            if (Directory.Exists(injectRomPath))
            {
                DirectoryCopy(injectRomPath, "test", true);
                //TurboGrafCD
                using (Process TurboInject = new Process())
                {
                    TurboInject.StartInfo.UseShellExecute = false;
                    TurboInject.StartInfo.CreateNoWindow = true;
                    TurboInject.StartInfo.FileName = Path.Combine(toolsPath, "BuildTurboCDPcePkg.exe");
                    TurboInject.StartInfo.Arguments = $"test";
                    TurboInject.Start();
                    TurboInject.WaitForExit();
                }
                Directory.Delete("test", true);
            }
            else
            {
                //creating pkg file including the TG16 rom
                using (Process TurboInject = new Process())
                {
                    TurboInject.StartInfo.UseShellExecute = false;
                    TurboInject.StartInfo.CreateNoWindow = true;
                    TurboInject.StartInfo.FileName = Path.Combine(toolsPath, "BuildPcePkg.exe");
                    TurboInject.StartInfo.Arguments = $"\"{injectRomPath}\"";
                    TurboInject.Start();
                    TurboInject.WaitForExit();
                }
            }
            
            //replacing tg16 rom
            File.Delete(Path.Combine(baseRomPath, "content", "pceemu", "pce.pkg"));
            File.Copy("pce.pkg", Path.Combine(baseRomPath, "content", "pceemu", "pce.pkg"));
            File.Delete("pce.pkg");
        }

        private static void NESSNES(string injectRomPath)
        {
            string rpxFile = Directory.GetFiles(Path.Combine(baseRomPath, "code"), "*.rpx")[0]; //To get the RPX path where the NES/SNES rom needs to be Injected in

            RPXdecomp(rpxFile); //Decompresses the RPX to be able to write the game into it

            using (Process retroinject = new Process())
            {
                retroinject.StartInfo.UseShellExecute = false;
                retroinject.StartInfo.CreateNoWindow = true;
                retroinject.StartInfo.RedirectStandardOutput = true;
                retroinject.StartInfo.RedirectStandardError = true;
                retroinject.StartInfo.FileName = Path.Combine(toolsPath, "retroinject.exe");
                retroinject.StartInfo.Arguments = $"\"{rpxFile}\" \"{injectRomPath}\" \"{rpxFile}\"";

                retroinject.Start();
                retroinject.WaitForExit();
                var s = retroinject.StandardOutput.ReadToEnd();
                var e = retroinject.StandardError.ReadToEnd();
                if (e.Contains("is too large") || s.Contains("is too large"))
                {
                    throw new Exception("retro");
                }
               
            }

            RPXcomp(rpxFile); //Compresses the RPX
        }

        private static void GBA(string injectRomPath)
        {
            bool delete = false;
            if(!new FileInfo(injectRomPath).Extension.Contains("gba"))
            {
                //it's a GBC or GB rom so it needs to be copied into goomba.gba and then padded to 32Mb (16 would work too but just ot be save)
                using (Process goomba = new Process())
                {
                    goomba.StartInfo.UseShellExecute = false;
                    goomba.StartInfo.CreateNoWindow = true;
                    goomba.StartInfo.FileName = "cmd.exe";
                    goomba.StartInfo.Arguments = $"/c copy /b \"{Path.Combine(toolsPath, "goomba.gba")}\"+\"{injectRomPath}\" \"{Path.Combine(toolsPath, "goombamenu.gba")}\"";

                    goomba.Start();
                    goomba.WaitForExit();
                }

                //padding
                byte[] rom = new byte[33554432];
                FileStream fs = new FileStream(Path.Combine(toolsPath, "goombamenu.gba"), FileMode.Open);
                fs.Read(rom, 0, (int)fs.Length);
                fs.Close();
                File.WriteAllBytes(Path.Combine(toolsPath, "goombaPadded.gba"), rom);
                Console.ReadLine();
                injectRomPath = Path.Combine(toolsPath, "goombaPadded.gba");
                delete = true;
            }


            using (Process psb = new Process())
            {
                psb.StartInfo.UseShellExecute = false;
                psb.StartInfo.CreateNoWindow = true;
                psb.StartInfo.FileName = Path.Combine(toolsPath, "psb.exe");
                psb.StartInfo.Arguments = $"\"{Path.Combine(baseRomPath, "content", "alldata.psb.m")}\" \"{injectRomPath}\" \"{Path.Combine(baseRomPath, "content", "alldata.psb.m")}\"";

                psb.Start();
                psb.WaitForExit();
            }
            if (delete)
            {
                File.Delete(injectRomPath);
                File.Delete(Path.Combine(toolsPath, "goombamenu.gba"));
            }
        }
        private static void DownloadSysTitle(MainViewModel mvm)
        {
            if (mvm.SysKeyset() && mvm.SysKey1set())
            {
                using (Process download = new Process())
                {
                    download.StartInfo.FileName = Path.Combine(toolsPath, "WiiUDownloader.exe");
                    download.StartInfo.Arguments = $"0005001010004001 {Properties.Settings.Default.SysKey} \"{Path.Combine(tempPath, "download")}\"";

                    download.Start();
                    download.WaitForExit();
                }
                using (Process decrypt = new Process())
                {
                    decrypt.StartInfo.FileName = Path.Combine(toolsPath, "Cdecrypt.exe");
                    decrypt.StartInfo.Arguments = $"{Properties.Settings.Default.Ckey} \"{Path.Combine(tempPath, "download")}\" \"{Path.Combine(Properties.Settings.Default.BasePath, $"vwiisys")}\"";

                    decrypt.Start();
                    decrypt.WaitForExit();
                }
                using (Process download = new Process())
                {
                    Directory.Delete(Path.Combine(tempPath, "download"), true);
                    download.StartInfo.FileName = Path.Combine(toolsPath, "WiiUDownloader.exe");
                    download.StartInfo.Arguments = $"0005001010004000 {Properties.Settings.Default.SysKey1} \"{Path.Combine(tempPath, "download")}\"";

                    download.Start();
                    download.WaitForExit();
                }
                using (Process decrypt = new Process())
                {
                    decrypt.StartInfo.FileName = Path.Combine(toolsPath, "Cdecrypt.exe");
                    decrypt.StartInfo.Arguments = $"{Properties.Settings.Default.Ckey} \"{Path.Combine(tempPath, "download")}\" \"{Path.Combine(tempPath, "tempd")}\"";

                    decrypt.Start();
                    decrypt.WaitForExit();
                    File.Copy(Path.Combine(tempPath, "tempd", "code", "font.bin"), Path.Combine(Properties.Settings.Default.BasePath, $"vwiisys", "code", "font.bin"));
                    File.Copy(Path.Combine(tempPath, "tempd", "code", "deint.txt"), Path.Combine(Properties.Settings.Default.BasePath, $"vwiisys", "code", "deint.txt"));
                    File.Delete(Path.Combine(Properties.Settings.Default.BasePath, $"vwiisys", "code", "app.xml"));
                }
            }
        }
        private static void NDS(string injectRomPath)
        {
            
            string RomName = string.Empty;
            using (Process getRomName = new Process())
            {
                getRomName.StartInfo.UseShellExecute = false;
                getRomName.StartInfo.CreateNoWindow = false;
                getRomName.StartInfo.RedirectStandardOutput = true;
                getRomName.StartInfo.FileName = "cmd.exe";
                Console.WriteLine(Directory.GetCurrentDirectory());
                //getRomName.StartInfo.Arguments = $"/c \"Tools\\7za.exe\" l \"temp\\baserom\\content\\0010\\rom.zip\" | findstr \"WUP\"";
                getRomName.StartInfo.Arguments = "/c Tools\\7za.exe l temp\\baserom\\content\\0010\\rom.zip | findstr WUP";
                getRomName.Start();
                getRomName.WaitForExit();
                var s = getRomName.StandardOutput.ReadToEnd();
                var split = s.Split(' ');
                RomName = split[split.Length - 1].Replace("\r\n", "");
            }
            using (Process RomEdit = new Process())
            {
                RomEdit.StartInfo.UseShellExecute = false;
                RomEdit.StartInfo.CreateNoWindow = true;
                RomEdit.StartInfo.RedirectStandardOutput = true;
                RomEdit.StartInfo.FileName = Path.Combine(toolsPath, "7za.exe");
                //d Path.Combine(baseRomPath, "content", "0010", "rom.zip")
                RomEdit.StartInfo.Arguments = $"d temp\\baserom\\content\\0010\\rom.zip";
                RomEdit.Start();
                RomEdit.WaitForExit();
                File.Copy(injectRomPath, $"{RomName}");
                RomEdit.StartInfo.Arguments = $"u temp\\baserom\\content\\0010\\rom.zip {RomName}";
                RomEdit.Start();
                RomEdit.WaitForExit();
            }
            File.Delete(RomName);

        }

    
        private static void N64(string injectRomPath, N64Conf config)
        {
            string mainRomPath = Directory.GetFiles(Path.Combine(baseRomPath, "content", "rom"))[0];
            string mainIni = Path.Combine(baseRomPath, "content", "config", $"{Path.GetFileName(mainRomPath)}.ini");
            using (Process n64convert = new Process())
            {
                n64convert.StartInfo.UseShellExecute = false;
                n64convert.StartInfo.CreateNoWindow = true;
                n64convert.StartInfo.FileName = Path.Combine(toolsPath, "N64Converter.exe");
                n64convert.StartInfo.Arguments = $"\"{injectRomPath}\" \"{mainRomPath}\"";

                n64convert.Start();
                n64convert.WaitForExit();
            }
            if(config.INIBin == null)
            {
                if (config.INIPath == null)
                {
                    File.Delete(mainIni);
                    File.Copy(Path.Combine(toolsPath, "blank.ini"), mainIni);
                }
                else
                {
                    File.Delete(mainIni);
                    File.Copy(config.INIPath, mainIni);
                }
            }
            else
            {
                ReadFileFromBin(config.INIBin, "custom.ini");
                File.Delete(mainIni);
                File.Move("custom.ini", mainIni);
            }
            

            if (config.DarkFilter)
            {
                string filePath = Path.Combine(baseRomPath, "content", "FrameLayout.arc");
                using (BinaryWriter writer = new BinaryWriter(new FileStream(filePath, FileMode.Open)))
                {
                    writer.Seek(0x1AD8, SeekOrigin.Begin);
                    writer.Write(0L);
                }
            }
        }

        //Compressed or decompresses the RPX using wiiurpxtool
        private static void RPXdecomp(string rpxpath)
        {
            using (Process rpxtool = new Process())
            {
                rpxtool.StartInfo.UseShellExecute = false;
                rpxtool.StartInfo.CreateNoWindow = true;
                rpxtool.StartInfo.FileName = Path.Combine(toolsPath, "wiiurpxtool.exe");
                rpxtool.StartInfo.Arguments = $"-d \"{rpxpath}\"";

                rpxtool.Start();
                rpxtool.WaitForExit();
            }
        }

        private static void RPXcomp(string rpxpath)
        {
            using (Process rpxtool = new Process())
            {
                rpxtool.StartInfo.UseShellExecute = false;
                rpxtool.StartInfo.CreateNoWindow = true;
                rpxtool.StartInfo.FileName = Path.Combine(toolsPath, "wiiurpxtool.exe");
                rpxtool.StartInfo.Arguments = $"-c \"{rpxpath}\"";

                rpxtool.Start();
                rpxtool.WaitForExit();
            }
        }

        private static void ReadFileFromBin(byte[] bin, string output)
        {
            File.WriteAllBytes(output, bin);
        }
        private static void Images(GameConfig config)
        {
            try
            {


                //is an image embedded? yes => export them and check for issues
                //no => using path
                if (Directory.Exists(imgPath)) // sanity check
                {
                    Directory.Delete(imgPath, true);
                }
                Directory.CreateDirectory(imgPath);
                //ICON
                List<bool> Images = new List<bool>();
                if (config.TGAIco.ImgBin == null)
                {
                    //use path
                    if (config.TGAIco.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGAIco.ImgPath, Path.Combine(imgPath), false, 128,128,32, "iconTex.tga");
                    }
                    else
                    {
                        Images.Add(false);
                    }
                }
                else
                {
                    ReadFileFromBin(config.TGAIco.ImgBin, $"iconTex.{config.TGAIco.extension}");
                    CopyAndConvertImage($"iconTex.{config.TGAIco.extension}", Path.Combine(imgPath), true, 128, 128, 32, "iconTex.tga");
                    Images.Add(true);
                }

                //Drc
                if (config.TGADrc.ImgBin == null)
                {
                    //use path
                    if (config.TGADrc.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGADrc.ImgPath, Path.Combine(imgPath), false, 854,480,24, "bootDrcTex.tga");
                    }
                    else
                    {
                        Images.Add(false);
                    }
                }
                else
                {
                    ReadFileFromBin(config.TGADrc.ImgBin, $"bootDrcTex.{config.TGADrc.extension}");
                    CopyAndConvertImage($"bootDrcTex.{config.TGADrc.extension}", Path.Combine(imgPath), true,854,480,24, "bootDrcTex.tga");
                    Images.Add(true);
                }

                //tv
                if (config.TGATv.ImgBin == null)
                {
                    //use path
                    if (config.TGATv.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGATv.ImgPath, Path.Combine(imgPath), false,1280,720,24, "bootTvTex.tga");
                    }
                    else
                    {
                        Images.Add(false);
                    }
                }
                else
                {
                    ReadFileFromBin(config.TGATv.ImgBin, $"bootTvTex.{config.TGATv.extension}");
                    CopyAndConvertImage($"bootTvTex.{config.TGATv.extension}", Path.Combine(imgPath), true, 1280, 720, 24, "bootTvTex.tga");
                    Images.Add(true);
                }

                //logo
                if (config.TGALog.ImgBin == null)
                {
                    //use path
                    if (config.TGALog.ImgPath != null)
                    {
                        Images.Add(true);
                        CopyAndConvertImage(config.TGALog.ImgPath, Path.Combine(imgPath), false, 170,42,32, "bootLogoTex.tga");
                    }
                    else
                    {
                        Images.Add(false);
                    }
                }
                else
                {
                    ReadFileFromBin(config.TGALog.ImgBin, $"bootLogoTex.{config.TGALog.extension}");
                    CopyAndConvertImage($"bootLogoTex.{config.TGALog.extension}", Path.Combine(imgPath), true, 170, 42, 32, "bootLogoTex.tga");
                    Images.Add(true);
                }

                //Fixing Images + Injecting them
                if (Images[0] || Images[1] || Images[2] || Images[3])
                {
                    using (Process checkIfIssue = new Process())
                    {
                        checkIfIssue.StartInfo.UseShellExecute = false;
                        checkIfIssue.StartInfo.CreateNoWindow = false;
                        checkIfIssue.StartInfo.RedirectStandardOutput = true;
                        checkIfIssue.StartInfo.RedirectStandardError = true;
                        checkIfIssue.StartInfo.FileName = $"{Path.Combine(toolsPath,"tga_verify.exe")}";
                        Console.WriteLine(Directory.GetCurrentDirectory());
                        checkIfIssue.StartInfo.Arguments = $"\"{imgPath}\"";
                        checkIfIssue.Start();
                        checkIfIssue.WaitForExit();
                        var s = checkIfIssue.StandardOutput.ReadToEnd();
                        if (s.Contains("width") || s.Contains("height") || s.Contains("depth"))
                        {
                            throw new Exception("Size");
                        }
                        var e = checkIfIssue.StandardError.ReadToEnd();
                        if (e.Contains("width") || e.Contains("height") || e.Contains("depth"))
                        {
                            throw new Exception("Size");
                        }
                        if (e.Contains("TRUEVISION") || s.Contains("TRUEVISION"))
                        {
                            checkIfIssue.StartInfo.UseShellExecute = false;
                            checkIfIssue.StartInfo.CreateNoWindow = false;
                            checkIfIssue.StartInfo.RedirectStandardOutput = true;
                            checkIfIssue.StartInfo.RedirectStandardError = true;
                            checkIfIssue.StartInfo.FileName = $"{Path.Combine(toolsPath, "tga_verify.exe")}";
                            Console.WriteLine(Directory.GetCurrentDirectory());
                            checkIfIssue.StartInfo.Arguments = $"--fixup \"{imgPath}\"";
                            checkIfIssue.Start();
                            checkIfIssue.WaitForExit();
                        }
                        Console.ReadLine();
                    }

                    if (Images[2])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "bootTvTex.tga"));
                        File.Move(Path.Combine(imgPath, "bootTvTex.tga"), Path.Combine(baseRomPath, "meta", "bootTvTex.tga"));
                    }
                    if (Images[1])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "bootDrcTex.tga"));
                        File.Move(Path.Combine(imgPath, "bootDrcTex.tga"), Path.Combine(baseRomPath, "meta", "bootDrcTex.tga"));
                    }
                    if (Images[0])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "iconTex.tga"));
                        File.Move(Path.Combine(imgPath, "iconTex.tga"), Path.Combine(baseRomPath, "meta", "iconTex.tga"));
                    }
                    if (Images[3])
                    {
                        File.Delete(Path.Combine(baseRomPath, "meta", "bootLogoTex.tga"));
                        File.Move(Path.Combine(imgPath, "bootLogoTex.tga"), Path.Combine(baseRomPath, "meta", "bootLogoTex.tga"));
                    }
                }
            }
            catch(Exception e)
            {
                if (e.Message.Contains("Size"))
                {
                    throw e;
                }
                throw new Exception("Images");
            }

        }
        
        private static void CopyAndConvertImage(string inputPath, string outputPath, bool delete, int widht, int height, int bit, string newname)
        {
            if (inputPath.EndsWith(".tga"))
            {
                File.Copy(inputPath, Path.Combine(outputPath,newname));
            }
            else
            {
                using (Process png2tga = new Process())
                {
                    png2tga.StartInfo.UseShellExecute = false;
                    png2tga.StartInfo.CreateNoWindow = true;
                    png2tga.StartInfo.FileName = Path.Combine(toolsPath, "png2tga.exe");
                    png2tga.StartInfo.Arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" --width={widht} --height={height} --tga-bpp={bit} --tga-compression=none";

                    png2tga.Start();
                    png2tga.WaitForExit();
                }
                string name = Path.GetFileNameWithoutExtension(inputPath);
                if(File.Exists(Path.Combine(outputPath , name + ".tga")))
                {
                    File.Move(Path.Combine(outputPath, name + ".tga"), Path.Combine(outputPath, newname));
                }
            }
            if (delete)
            {
                File.Delete(inputPath);
            }
        }

        private static string RemoveHeader(string filePath)
        {
            // logic taken from snesROMUtil
            using (FileStream inStream = new FileStream(filePath, FileMode.Open))
            {
                byte[] header = new byte[512];
                inStream.Read(header, 0, 512);
                string string1 = BitConverter.ToString(header, 8, 3);
                string string2 = Encoding.ASCII.GetString(header, 0, 11);
                string string3 = BitConverter.ToString(header, 30, 16);
                if (string1 != "AA-BB-04" && string2 != "GAME DOCTOR" && string3 != "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00")
                    return filePath;

                string newFilePath = Path.Combine(tempPath, Path.GetFileName(filePath));
                using (FileStream outStream = new FileStream(newFilePath, FileMode.OpenOrCreate))
                {
                    inStream.CopyTo(outStream);
                }

                return newFilePath;
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            foreach (FileInfo file in dir.EnumerateFiles())
            {
                file.CopyTo(Path.Combine(destDirName, file.Name), false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dir.EnumerateDirectories())
                {
                    DirectoryCopy(subdir.FullName,  Path.Combine(destDirName, subdir.Name), copySubDirs);
                }
            }
        }
    }
}
