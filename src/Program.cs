// MIT licensed. No game binary is embedded. Windows/.NET Framework 4.x.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Toy Story 3 FPS Patcher")]
[assembly: System.Reflection.AssemblyVersion("1.1.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.1.0.0")]

namespace TS3FPS
{
    public interface IFiles
    {
        bool Exists(string path);
        byte[] Read(string path);
        void WriteNew(string path, byte[] data);
        void Replace(string temporary, string target);
        void Delete(string path);
    }
    public sealed class DiskFiles : IFiles
    {
        public bool Exists(string p) { return File.Exists(p); }
        public byte[] Read(string p) { return File.ReadAllBytes(p); }
        public void WriteNew(string p, byte[] b)
        {
            bool created = false;
            try
            {
                using (FileStream f = new FileStream(p, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { created = true; f.Write(b, 0, b.Length); f.Flush(true); }
            }
            catch
            {
                if (created) { try { File.Delete(p); } catch { } }
                throw;
            }
        }
        public void Replace(string p, string target) { File.Replace(p, target, null); }
        public void Delete(string p) { if (File.Exists(p)) File.Delete(p); }
    }
    public static class Engine
    {
        public const string Name = "Toy Story 3 Complete Edition.exe";
        public const string OriginalHash = "f87941b418a56b935a5e8372c11720304410d204a8cabd1d9970becfceff9722";
        public const int Size = 10712576;
        public const int Offset = 0x7ED810;
        public static readonly SortedDictionary<int, string> Hashes = new SortedDictionary<int, string>
        {
            { 75, "68589c664bb90ddf95cd8a0a4674aa0e5b8f583326477e3074a999137a43a01a" },
            { 90, "7443191d03d5568b2bf9f61d19170e35211e12b9fc1aff9b503286a8a5120030" },
            { 100, "120e36dc9456d0f6dea4f8d83af33f4808d3cb8a3ffc433ede6544ba4eed88d9" },
            { 120, "d2c6db9c13e332359e155fc25ab54ed838399eecfa9baa34cb493fb81289cb9d" },
            { 144, "bc1d9154fecdd1caa67d10cf447762ce3efdc63eac41b80680216f9765798784" },
            { 165, "10c59ccf917bf13c8dfd3d5007806ecca56914754b3d0746fae6642241a51e9c" },
            { 180, "c341109c624d7903b76b849dd8e2fb6726cbd4b0f79caf7d2f1aceaadc4925b3" },
            { 200, "13f8995bc25689d862d4cd4860ee50d609a88266597df354d568012277c0cf37" },
            { 240, "4161f03e0d9ce76a63dbb2bef1b7e1a8ef634d4b4fad0ded0ccf24b5871fb54d" },
            { 277, "2cf15ea6885d9f4018cd7c09ebb8e695d7559ed6c6e419c1c005c8d3b49f8019" },
            { 280, "5bf8481b079641ab922fab399aef44b6decff3cad7b973fcfb4519df7e8a1863" },
            { 300, "5fdd011d13e9a0dc57caaaa69c3c3f5db1b2844ea09733072bfa9280c865b2bd" },
            { 360, "4e48f63a7bba08f6851b292f8f3f9c66ae62ce27a165407d345c7dd586ed1b20" },
        };
        public static string Hash(byte[] b)
        {
            using (SHA256 h = SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
        }
        public static int Identify(byte[] b)
        {
            if (b.Length != Size) throw new InvalidDataException("Unsupported executable size. No game files were changed.");
            string hash = Hash(b);
            if (hash == OriginalHash) return 60;
            foreach (KeyValuePair<int, string> x in Hashes) if (hash == x.Value) return x.Key;
            throw new InvalidDataException("Unsupported or modified executable. This FPS-only tool accepts only the exact original build or its own FPS variants. Graphics-modified builds are not supported.");
        }
        public static byte[] Clean(byte[] b)
        {
            Identify(b);
            byte[] clean = (byte[])b.Clone();
            Buffer.BlockCopy(BitConverter.GetBytes(1.0 / 60.0), 0, clean, Offset, 8);
            if (Hash(clean) != OriginalHash || BitConverter.ToDouble(clean, Offset + 8) != 1.0 / 30.0)
                throw new InvalidDataException("Original reconstruction failed verification.");
            return clean;
        }
        public static byte[] Transform(byte[] input, int fps)
        {
            if (fps != 60 && !Hashes.ContainsKey(fps)) throw new ArgumentException("Unsupported FPS target.");
            byte[] clean = Clean(input);
            byte[] result = (byte[])clean.Clone();
            Buffer.BlockCopy(BitConverter.GetBytes(1.0 / fps), 0, result, Offset, 8);
            for (int i = 0; i < result.Length; ++i)
                if ((i < Offset || i >= Offset + 8) && result[i] != clean[i])
                    throw new InvalidDataException("Unexpected change outside the limiter interval.");
            if (Hash(result) != (fps == 60 ? OriginalHash : Hashes[fps]))
                throw new InvalidDataException("Output verification failed.");
            return result;
        }
        public static string Change(IFiles files, string path, int fps)
        {
            byte[] before = files.Read(path);
            int current = Identify(before);
            byte[] desired = Transform(before, fps);
            if (current == fps) return fps == 60 ? "The original executable is already installed." : "This FPS target is already installed.";
            byte[] clean = Clean(before);
            string backup = path + ".fps-patcher.original.bak";
            if (files.Exists(backup) && Hash(files.Read(backup)) != OriginalHash)
                throw new InvalidDataException("An existing backup has an unexpected hash. It will not be overwritten. Inspect or move that backup before continuing.");
            if (!files.Exists(backup)) files.WriteNew(backup, clean);
            if (Hash(files.Read(backup)) != OriginalHash)
                throw new IOException("Backup verification failed. The game executable was not replaced.");
            string temp = path + ".fps-patcher-" + Guid.NewGuid().ToString("N") + ".tmp";
            bool committed = false;
            try
            {
                files.WriteNew(temp, desired);
                if (Hash(files.Read(temp)) != Hash(desired)) throw new IOException("Temporary file verification failed.");
                if (Hash(files.Read(path)) != Hash(before)) throw new IOException("The executable changed externally. Replacement cancelled.");
                files.Replace(temp, path);
                committed = true;
                if (Hash(files.Read(path)) != Hash(desired)) throw new IOException("Installed file verification failed.");
                return (fps == 60 ? "Original 60 FPS executable restored." : fps + " FPS applied. Select 60 FPS in the game menu.") + "\r\nVerified backup: " + backup;
            }
            catch (Exception error)
            {
                if (committed)
                {
                    string rollback = path + ".fps-rollback-" + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        files.WriteNew(rollback, clean);
                        files.Replace(rollback, path);
                        if (Hash(files.Read(path)) != OriginalHash) throw new IOException("Rollback verification failed.");
                        throw new IOException(error.Message + " The original executable was restored.", error);
                    }
                    catch (IOException rollbackError)
                    {
                        if (Hash(files.Read(path)) == OriginalHash) throw;
                        throw new IOException("Replacement verification failed and automatic rollback could not be completed. Restore the verified backup manually: " + backup, rollbackError);
                    }
                    finally { try { files.Delete(rollback); } catch { } }
                }
                throw;
            }
            finally { try { files.Delete(temp); } catch { } }
        }
    }
    public static class SteamSearch
    {
        static string Get(RegistryHive hive, RegistryView view, string key, string name)
        {
            try { using (RegistryKey b = RegistryKey.OpenBaseKey(hive, view)) using (RegistryKey k = b.OpenSubKey(key)) return k == null ? null : k.GetValue(name) as string; }
            catch { return null; }
        }
        public static IEnumerable<string> Libraries(string steam)
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (String.IsNullOrWhiteSpace(steam)) return paths;
            paths.Add(Path.GetFullPath(steam));
            foreach (string name in new string[] { "steamapps\\libraryfolders.vdf", "config\\libraryfolders.vdf" })
            {
                string vdf = Path.Combine(steam, name);
                if (!File.Exists(vdf)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\\\"(?:path|[0-9]+)\\\"\\s+\\\"([^\\\"]+)\\\""))
                {
                    string candidate = m.Groups[1].Value.Replace("\\\\", "\\");
                    if (Path.IsPathRooted(candidate)) paths.Add(Path.GetFullPath(candidate));
                }
            }
            return paths;
        }
        public static string Candidate(string library, string manifest)
        {
            Match m = Regex.Match(manifest, "\\\"installdir\\\"\\s+\\\"([^\\\"]+)\\\"");
            if (!m.Success) return null;
            string common = Path.GetFullPath(Path.Combine(library, "steamapps", "common")) + Path.DirectorySeparatorChar;
            string dir = Path.GetFullPath(Path.Combine(common, m.Groups[1].Value));
            if (!dir.StartsWith(common, StringComparison.OrdinalIgnoreCase)) return null;
            return Path.Combine(dir, Engine.Name);
        }
        public static List<string> Find()
        {
            HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string p in new string[] {
                Get(RegistryHive.CurrentUser, RegistryView.Default, @"Software\Valve\Steam", "SteamPath"),
                Get(RegistryHive.LocalMachine, RegistryView.Registry32, @"Software\Valve\Steam", "InstallPath"),
                Get(RegistryHive.LocalMachine, RegistryView.Registry64, @"Software\Valve\Steam", "InstallPath"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam") })
                if (!String.IsNullOrWhiteSpace(p)) roots.Add(p);
            HashSet<string> found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in roots)
            {
                try
                {
                    foreach (string lib in Libraries(root))
                    {
                        string manifest = Path.Combine(lib, "steamapps", "appmanifest_4049180.acf");
                        string path = File.Exists(manifest) ? Candidate(lib, File.ReadAllText(manifest)) : null;
                        if (path != null && File.Exists(path)) found.Add(path);
                        path = Path.Combine(lib, "steamapps", "common", "Toy Story 3 Complete Edition", Engine.Name);
                        if (File.Exists(path)) found.Add(path);
                    }
                }
                catch { }
            }
            return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
    public sealed class PatcherForm : Form
    {
        TextBox path = new TextBox();
        ComboBox fps = new ComboBox();
        TextBox status = new TextBox();
        Button apply = new Button(), restore = new Button(), browse = new Button(), detect = new Button();
        public PatcherForm(string initial)
        {
            Text = "Toy Story 3 - FPS Patcher";
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(690, 350); MinimumSize = new Size(640, 390);
            StartPosition = FormStartPosition.CenterScreen;
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 6, ColumnCount = 1 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.Controls.Add(new Label { Text = "FPS only - automatic original backup and verified restore", Dock = DockStyle.Fill }, 0, 0);
            TableLayoutPanel fileRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90)); fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            path.Dock = DockStyle.Fill; path.ReadOnly = true; path.Text = initial ?? "";
            browse.Text = "Browse..."; browse.Dock = DockStyle.Fill; detect.Text = "Find Steam"; detect.Dock = DockStyle.Fill;
            fileRow.Controls.Add(path, 0, 0); fileRow.Controls.Add(browse, 1, 0); fileRow.Controls.Add(detect, 2, 0); layout.Controls.Add(fileRow, 0, 1);
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            buttons.Controls.Add(new Label { Text = "Target FPS:", AutoSize = true, Padding = new Padding(0, 7, 8, 0) });
            fps.DropDownStyle = ComboBoxStyle.DropDownList; fps.Width = 90;
            foreach (int cap in Engine.Hashes.Keys) fps.Items.Add(cap); fps.SelectedItem = 120;
            apply.Text = "Apply FPS"; apply.AutoSize = true; restore.Text = "Restore original"; restore.AutoSize = true;
            buttons.Controls.Add(fps); buttons.Controls.Add(apply); buttons.Controls.Add(restore); layout.Controls.Add(buttons, 0, 2);
            layout.Controls.Add(new Label { Text = "Close the game first. In-game, select 60 FPS to use the patched target.\r\nThe 30 FPS option is unchanged. Graphics-modified EXEs are rejected.", Dock = DockStyle.Fill }, 0, 3);
            status.Multiline = true; status.ReadOnly = true; status.Dock = DockStyle.Fill; status.ScrollBars = ScrollBars.Vertical; layout.Controls.Add(status, 0, 4);
            layout.Controls.Add(new Label { Text = "Unofficial | v1.1 | 120/277 previously reported working; other targets not game-tested", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8F) }, 0, 5);
            Controls.Add(layout);
            browse.Click += delegate {
                using (OpenFileDialog d = new OpenFileDialog { Title = "Select the game executable", Filter = "Game executable|*.exe", FileName = Engine.Name })
                { if (d.ShowDialog(this) == DialogResult.OK) { path.Text = d.FileName; Inspect(); } }
            };
            detect.Click += delegate { Find(); };
            apply.Click += delegate { Change((int)fps.SelectedItem); };
            restore.Click += delegate { Change(60); };
            Shown += delegate { if (String.IsNullOrWhiteSpace(path.Text)) Find(); else Inspect(); };
        }
        void Find()
        {
            try
            {
                List<string> files = SteamSearch.Find();
                if (files.Count == 0) { status.Text = "Game not found automatically. Click Browse and select the game executable."; return; }
                path.Text = files[0]; Inspect();
                if (files.Count > 1) status.AppendText("\r\nMultiple installations found. Check the selected path or use Browse.");
            }
            catch (Exception e) { status.Text = "Automatic search failed: " + e.Message + "\r\nUse Browse to select the executable."; }
        }
        void Inspect()
        {
            try
            {
                int cap = Engine.Identify(File.ReadAllBytes(path.Text));
                status.Text = cap == 60 ? "Supported original build detected. Choose FPS and click Apply." : "Supported FPS-only build detected: " + cap + " FPS. You can change target or restore the original.";
                if (cap != 60) fps.SelectedItem = cap;
            }
            catch (Exception e) { status.Text = e.Message; }
        }
        void Change(int cap)
        {
            if (String.IsNullOrWhiteSpace(path.Text)) { status.Text = "Select the game executable first."; return; }
            try
            {
                Process[] running = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Engine.Name));
                bool active = running.Length > 0;
                foreach (Process p in running) p.Dispose();
                if (active) { status.Text = "Close the game before applying or restoring."; return; }
                if ((File.GetAttributes(path.Text) & FileAttributes.ReparsePoint) != 0) throw new IOException("Select the real executable, not a file link.");
                apply.Enabled = restore.Enabled = browse.Enabled = detect.Enabled = false; UseWaitCursor = true;
                status.Text = Engine.Change(new DiskFiles(), Path.GetFullPath(path.Text), cap);
            }
            catch (UnauthorizedAccessException)
            {
                status.Text = "Windows denied write access. You can restart this patcher as administrator. The backup, if created, remains beside the game executable.";
                if (MessageBox.Show(this, "Restart the patcher as administrator? Windows will ask for permission. Click Apply again after it restarts.", "Write permission required", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--select \"" + path.Text + "\"") { UseShellExecute = true, Verb = "runas" }); Close(); }
                    catch (Exception e) { status.AppendText("\r\nRestart cancelled or failed: " + e.Message); }
                }
            }
            catch (Exception e) { status.Text = e.Message; }
            finally { apply.Enabled = restore.Enabled = browse.Enabled = detect.Enabled = true; UseWaitCursor = false; }
        }
    }
    public sealed class MemoryFiles : IFiles
    {
        public Dictionary<string,byte[]> Data = new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
        public int Writes;
        public bool FailReplace, CorruptFirstReplace;
        public bool Exists(string p) { return Data.ContainsKey(p); }
        public byte[] Read(string p) { return (byte[])Data[p].Clone(); }
        public void WriteNew(string p, byte[] b) { if (Exists(p)) throw new IOException("Existing file"); Data.Add(p, (byte[])b.Clone()); ++Writes; }
        public void Delete(string p) { Data.Remove(p); }
        public void Replace(string p, string target)
        {
            if (FailReplace) throw new IOException("Simulated replacement failure");
            Data[target] = Read(p); Delete(p);
            if (CorruptFirstReplace) { CorruptFirstReplace = false; Data[target][0] ^= 1; }
        }
    }
    internal static class Program
    {
        static void Assert(bool condition, string error) { if (!condition) throw new Exception(error); }
        static int SelfTest(string originalPath)
        {
            byte[] original = File.ReadAllBytes(originalPath);
            Assert(Engine.Hash(original) == Engine.OriginalHash, "Invalid test fixture");
            foreach (int cap in Engine.Hashes.Keys)
            {
                byte[] changed = Engine.Transform(original, cap);
                Assert(Engine.Identify(changed) == cap, "Target identification");
                Assert(Engine.Hash(Engine.Transform(changed, 60)) == Engine.OriginalHash, "Restore bytes");
                MemoryFiles fs = new MemoryFiles(); fs.Data["game.exe"] = original;
                Engine.Change(fs, "game.exe", cap);
                Assert(Engine.Hash(fs.Read("game.exe")) == Engine.Hashes[cap], "Installed bytes");
                Assert(Engine.Hash(fs.Read("game.exe.fps-patcher.original.bak")) == Engine.OriginalHash, "Backup bytes");
                Engine.Change(fs, "game.exe", 60);
                Assert(Engine.Hash(fs.Read("game.exe")) == Engine.OriginalHash, "Restored transaction");
                MemoryFiles recover = new MemoryFiles(); recover.Data["game.exe"] = changed;
                Engine.Change(recover, "game.exe", 60);
                Assert(Engine.Hash(recover.Read("game.exe")) == Engine.OriginalHash, "Recovery without backup");
            }
            MemoryFiles unknown = new MemoryFiles(); byte[] invalid = (byte[])original.Clone(); invalid[10] ^= 1; unknown.Data["game.exe"] = invalid;
            try { Engine.Change(unknown, "game.exe", 120); throw new Exception("Accepted unknown build"); } catch (InvalidDataException) { }
            Assert(unknown.Writes == 0, "Unknown build wrote files");
            MemoryFiles badBackup = new MemoryFiles(); badBackup.Data["game.exe"] = original; badBackup.Data["game.exe.fps-patcher.original.bak"] = invalid;
            try { Engine.Change(badBackup, "game.exe", 120); throw new Exception("Accepted invalid backup"); } catch (InvalidDataException) { }
            Assert(badBackup.Writes == 0, "Bad backup overwritten");
            MemoryFiles fail = new MemoryFiles(); fail.Data["game.exe"] = original; fail.FailReplace = true;
            try { Engine.Change(fail, "game.exe", 120); throw new Exception("Simulated failure ignored"); } catch (IOException) { }
            Assert(Engine.Hash(fail.Read("game.exe")) == Engine.OriginalHash && fail.Data.Count == 2, "Failed transaction cleanup");
            MemoryFiles corrupt = new MemoryFiles(); corrupt.Data["game.exe"] = original; corrupt.CorruptFirstReplace = true;
            try { Engine.Change(corrupt, "game.exe", 120); throw new Exception("Corruption ignored"); } catch (IOException) { }
            Assert(Engine.Hash(corrupt.Read("game.exe")) == Engine.OriginalHash && corrupt.Data.Count == 2, "Rollback failed");
            string candidate = SteamSearch.Candidate(@"D:\SteamLibrary", "\"installdir\" \"Toy Story 3 Complete Edition\"");
            Assert(candidate == @"D:\SteamLibrary\steamapps\common\Toy Story 3 Complete Edition\Toy Story 3 Complete Edition.exe", "Manifest path");
            Assert(SteamSearch.Candidate(@"D:\SteamLibrary", "\"installdir\" \"..\\..\\elsewhere\"") == null, "Traversal accepted");
            return 0;
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                if (!BitConverter.IsLittleEndian) return 2;
                if (args.Length == 2 && args[0] == "--self-test") return SelfTest(args[1]);
                if (args.Length == 1 && args[0] == "--find-only")
                { foreach (string found in SteamSearch.Find()) Console.WriteLine(found); return 0; }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PatcherForm(args.Length == 2 && args[0] == "--select" ? args[1] : null));
                return 0;
            }
            catch (Exception e)
            {
                if (args.Length > 0 && args[0] == "--self-test") { Console.Error.WriteLine(e.ToString()); return 1; }
                MessageBox.Show(e.Message, "FPS Patcher", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1;
            }
        }
    }
}
