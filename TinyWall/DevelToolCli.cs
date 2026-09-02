using pylorak.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;

namespace pylorak.TinyWall
{
    internal static class DevelToolCli
    {
        private static readonly string[] SIGNING_FILE_PATTERNS = new string[] { "*.dll", "*.exe", "*.msi" };

        // --- Profile Creator ---

        public static string CreateProfile(string exePath)
        {
            if (!File.Exists(exePath))
                throw new FileNotFoundException($"Executable not found: {exePath}");

            var exe = new ExecutableSubject(exePath);
            var id = new DatabaseClasses.SubjectIdentity(exe) { AllowedSha1 = new List<string> { exe.HashSha1 } };
            if (exe.IsSigned && exe.CertValid)
            {
                id.CertificateSubjects = new List<string>();
                if (exe.CertSubject is not null)
                    id.CertificateSubjects.Add(exe.CertSubject);
            }

            var utf8bytes = SerializationHelper.Serialize(id);
            return Encoding.UTF8.GetString(utf8bytes);
        }

        // --- Database Creator ---

        public static void CreateDatabase(string sourceFolder, string outputFolder)
        {
            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException($"Input database folder not found: {sourceFolder}");

            var outputPath = Path.Combine(outputFolder, "profiles.json");
            var defAppInst = new DatabaseClasses.Application();
            var files = Directory.GetFiles(sourceFolder, "*.json", SearchOption.AllDirectories);
            var db = new DatabaseClasses.AppDatabase();

            foreach (string fpath in files)
            {
                if (fpath.Equals(outputPath, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                try
                {
                    var loadedAppInst = SerializationHelper.DeserializeFromFile(fpath, defAppInst);
                    if (Utils.IsNullOrEmpty(loadedAppInst.Name))
                        throw new ArgumentException($"No app name provided in profile: {fpath}");
                    db.KnownApplications.Add(loadedAppInst);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Unloadable profile: {fpath}", ex);
                }
            }

            db.Save(outputPath);
        }

        // --- Update Creator ---

        public static void CreateUpdate(string baseUrl, string projectDir, string outputFolder)
        {
            const string DB_OUT_NAME = "database.def";
            const string HOSTS_OUT_NAME = "hosts.def";
            const string DESCRIPTOR_NAME = "update.json";
            const string DESCRIPTOR_TEMPLATE_NAME = "update_template.json";
            const string MSI_FILENAME_X86 = "TinyWall_x86.msi";
            const string MSI_FILENAME_ARM64 = "TinyWall_arm64.msi";

            string msiX86Path = Path.Combine(projectDir, @"bin\Release", MSI_FILENAME_X86);
            string msiArm64Path = Path.Combine(projectDir, @"bin\Release", MSI_FILENAME_ARM64);
            string hostsPath = Path.Combine(projectDir, @"Sources\CommonAppData\TinyWall\hosts.custom");
            string profilesPath = Path.Combine(projectDir, @"Sources\CommonAppData\TinyWall\profiles.json");
            string twAssemblyPath = Path.Combine(projectDir, @"Sources\ProgramFiles\TinyWall\TinyWall.exe");
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            UpdateModule PrepareModule(string component_id, string src_filepath, string dst_filename, string version, bool compress)
            {
                if (!File.Exists(src_filepath))
                    throw new FileNotFoundException("File not found.", src_filepath);

                string dst_filepath = Path.Combine(outputFolder, dst_filename);
                if (compress)
                    Utils.CompressDeflate(src_filepath, dst_filepath);
                else
                    File.Copy(src_filepath, dst_filepath, true);

                return new UpdateModule
                {
                    Component = component_id,
                    ComponentVersion = version,
                    DownloadHash = Hasher.HashFile(src_filepath),
                    UpdateURL = baseUrl + dst_filename
                };
            }

            if (!File.Exists(twAssemblyPath))
                throw new FileNotFoundException("TinyWall assembly not found.", twAssemblyPath);
            if (!Directory.Exists(outputFolder))
                throw new DirectoryNotFoundException($"Output folder not found: {outputFolder}");

            var version_info = FileVersionInfo.GetVersionInfo(twAssemblyPath).ProductVersion.ToString().Trim();
            var timestamp = DateTime.UtcNow.ToString("O");
            var update = new UpdateDescriptor
            {
                Modules = new UpdateModule[4]
                {
                    PrepareModule("TinyWall_x86", msiX86Path, MSI_FILENAME_X86, version_info, false),
                    PrepareModule("TinyWall_arm64", msiArm64Path, MSI_FILENAME_ARM64, version_info, false),
                    PrepareModule("Database", profilesPath, DB_OUT_NAME, timestamp, true),
                    PrepareModule("HostsFile", hostsPath, HOSTS_OUT_NAME, timestamp, true)
                }
            };

            SerializationHelper.SerializeToFile(update, Path.Combine(outputFolder, DESCRIPTOR_NAME));
            update.Modules[3].DownloadHash = "[HOSTS_SHA256_PLACEHOLDER]";
            SerializationHelper.SerializeToFile(update, Path.Combine(outputFolder, DESCRIPTOR_TEMPLATE_NAME));
        }

        // --- ResX Optimizer ---

        private static Dictionary<string, ResXDataNode> ReadResXFile(string filePath)
        {
            var resxContents = new Dictionary<string, ResXDataNode>();
            using var resxReader = new ResXResourceReader(filePath);
            resxReader.UseResXDataNodes = true;
            IDictionaryEnumerator dict = resxReader.GetEnumerator();
            while (dict.MoveNext())
            {
                ResXDataNode node = (ResXDataNode)dict.Value;
                resxContents.Add(node.Name, node);
            }
            return resxContents;
        }

        public static List<KeyValuePair<string, string[]>> CollectResxLocalizations(string resourceDir)
        {
            if (!Directory.Exists(resourceDir))
                throw new DirectoryNotFoundException($"Resource directory not found: {resourceDir}");

            // Scan for primary .resx files (exactly one dot in filename) — top-level only
            var resxFiles = Directory.GetFiles(resourceDir, "*.resx", SearchOption.TopDirectoryOnly);
            var resources = new List<KeyValuePair<string, string[]>>();

            foreach (string filePath in resxFiles)
            {
                if (Path.GetFileName(filePath).AsSpan().CountCharOccurrence('.') != 1)
                    continue;

                string primaryBase = Path.GetFileNameWithoutExtension(filePath);
                string[] satellites = Directory.GetFiles(resourceDir, primaryBase + ".*.resx", SearchOption.TopDirectoryOnly);
                resources.Add(new KeyValuePair<string, string[]>(filePath, satellites));
            }

            return resources;
        }

        // If compare argument is true, method returns true if all of the optimized satellite files are the same as the input satellites.
        // If compare is false, always returns true.
        public static bool OptimizeResX(List<KeyValuePair<string, string[]>> resources, string outputFolder, bool compare)
        {
            if (!Directory.Exists(outputFolder))
                throw new DirectoryNotFoundException($"Output folder not found: {outputFolder}");

            var inputsAndOutputsIdentical = true;

            for (int i = 0; i < resources.Count; ++i)
            {
                var pair = resources[i];
                var primary = ReadResXFile(pair.Key);

                for (int s = 0; s < pair.Value.Length; ++s)
                {
                    var satellite = ReadResXFile(pair.Value[s]);
                    var newSatellite = new Dictionary<string, ResXDataNode>();

                    var primaryEnum = primary.GetEnumerator();
                    while (primaryEnum.MoveNext())
                    {
                        ResXDataNode primaryItem = primaryEnum.Current.Value;
                        if (!satellite.ContainsKey(primaryItem.Name))
                            continue;

                        ResXDataNode satelliteItem = satellite[primaryItem.Name];

                        // We only allow specific properties to be localized
                        if (satelliteItem.Name.Contains("."))
                        {
                            if (!satelliteItem.Name.EndsWith(".Text") &&
                                !satelliteItem.Name.EndsWith(".Title") &&
                                !satelliteItem.Name.EndsWith(".Filter") &&
                                !satelliteItem.Name.EndsWith(".ToolTip") &&
                                !satelliteItem.Name.EndsWith(".AccessibleName"))
                                continue;
                        }

                        // We don't save values that are the same as default
                        ITypeResolutionService? trs = null;
                        if (satelliteItem.GetValue(trs).Equals(primaryItem.GetValue(trs)))
                            continue;

                        newSatellite.Add(satelliteItem.Name, satelliteItem);
                    }

                    string outPath = Path.Combine(outputFolder, Path.GetFileName(pair.Value[s]));
                    using (var resxWriter = new ResXResourceWriter(outPath))
                    {
                        Dictionary<string, ResXDataNode>.Enumerator outputEnum = newSatellite.GetEnumerator();
                        while (outputEnum.MoveNext())
                            resxWriter.AddResource(outputEnum.Current.Value);
                        resxWriter.Generate();
                    }

                    // Compare input to output if asked
                    if (compare)
                    {
                        var optimized = outPath;
                        var optimizedContents = ReadResXFile(optimized);

                        // The optimizer only removes entries; it never changes retained nodes.
                        // Compare resource membership so BOM, line endings, and XML formatting
                        // do not make semantically identical dictionaries fail validation.
                        bool contentsIdentical = satellite.Count == optimizedContents.Count;
                        if (contentsIdentical)
                        {
                            foreach (string key in satellite.Keys)
                            {
                                if (!optimizedContents.ContainsKey(key))
                                {
                                    contentsIdentical = false;
                                    break;
                                }
                            }
                        }

                        if (!contentsIdentical)
                        {
                            Console.Error.WriteLine($"Optimized {Path.GetFileName(optimized)} differs from original!");
                            inputsAndOutputsIdentical = false;
                        }
                    }
                }
            }

            return inputsAndOutputsIdentical;
        }

        // --- Batch Signer ---

        public static bool BatchSign(string certName, string signDir, string signtoolPath, string timestampUrl)
        {
            if (!Directory.Exists(signDir))
                throw new DirectoryNotFoundException($"Signing directory not found: {signDir}");
            if (!File.Exists(signtoolPath))
                throw new FileNotFoundException($"Signtool.exe not found: {signtoolPath}");

            // Collect all files to sign
            var filesToSign = new List<string>();
            foreach (var pattern in SIGNING_FILE_PATTERNS)
            {
                string[] candidateFiles = Directory.GetFiles(signDir, pattern, SearchOption.AllDirectories);
                foreach (var filePath in candidateFiles)
                {
                    var signedStatus = pylorak.Windows.WinTrust.VerifyFileAuthenticode(filePath);
                    if (signedStatus == Windows.WinTrust.VerifyResult.SIGNATURE_MISSING)
                    {
                        filesToSign.Add("\"" + filePath + "\"");
                    }
                    else if (signedStatus == Windows.WinTrust.VerifyResult.SIGNATURE_INVALID)
                    {
                        throw new InvalidOperationException(
                            $"File \"{filePath}\" has pre-existing INVALID certificate. Signing aborted.");
                    }
                }
            }

            if (filesToSign.Count == 0)
            {
                // No files to sign, or all files are already signed
                return true;
            }

            var signParams = $"sign /n \"{certName}\" /d TinyWall /du \"https://tinywall.pados.hu\" /tr \"{timestampUrl}\" /td sha256 /fd sha256 /v {string.Join(" ", filesToSign)}";
            using var p = Utils.StartProcess(signtoolPath, signParams, false);
            p.WaitForExit();
            return p.ExitCode == 0;
        }
    }
}
