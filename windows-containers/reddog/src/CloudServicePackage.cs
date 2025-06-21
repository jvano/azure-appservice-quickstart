using System;
using System.IO;
using System.Reflection;
using Microsoft.WindowsAzure.Packaging;
using Microsoft.WindowsAzure.Packaging.DataContract;

namespace CloudServiceBootstrapper
{
    public static class CloudServicePackage
    {
        public static void ExtractFromCspkg(string cspkgFile, string targetDir)
        {
            using (PackageStore ocgPkgStore = new OpcPackageStore(cspkgFile))
            {
                PackageExtractor pkgExtractor = new PackageExtractor(ocgPkgStore);

                pkgExtractor.ExtractContent("ServiceDefinition/ServiceDefinition.csdef", new FileInfo(Path.Combine(targetDir, "ServiceDefinition.csdef")));
                pkgExtractor.ExtractContent("ServiceDefinition/ServiceDefinition.rd", new FileInfo(Path.Combine(targetDir, "ServiceDefinition.rd")));
                pkgExtractor.ExtractContent("ServiceDefinition/servicedefinition.rdsc", new FileInfo(Path.Combine(targetDir, "servicedefinition.rdsc")));

                // The PackageExtractor in Azure SDK has a bug - No matter what directory you provide It just extractors to the disk root
                pkgExtractor.ExtractLayoutsEx("Roles", new DirectoryInfo(targetDir));
            }

            Console.WriteLine($"Extracted {cspkgFile} to {targetDir}.");
        }

        public static void ExtractLayoutsEx(this PackageExtractor extractor, string layoutPrefix, DirectoryInfo dstDir)
        {
            foreach (var layout in extractor.Layouts)
            {
                if (layout.StartsWith(layoutPrefix))
                {
                    var fullPath = GetFullPath(dstDir, layout);
                    var dirInfo = new DirectoryInfo(fullPath);
                    if (dirInfo.Exists)
                    {
                        dirInfo.Delete(true);
                    }

                    extractor.ExtractLayoutEx(layout, new DirectoryInfo(fullPath));
                }
            }
        }

        public static void ExtractLayoutEx(this PackageExtractor extractor, string layout, DirectoryInfo dstDir)
        {
            PackageDefinition packageDef = GetPackageDefinition(extractor);

            var fld = packageDef.PackageLayouts[layout];
            foreach (var fd in fld)
            {
                var filePath = fd.Key;
                var fileDesc = fd.Value;

                var dstFile = new FileInfo(Path.Combine(dstDir.FullName, GetRelativeFilePath(filePath)));
                extractor.ExtractContent(fileDesc.DataContentReference, dstFile);
                dstFile.IsReadOnly = fileDesc.ReadOnly;
                dstFile.CreationTimeUtc = fileDesc.CreatedTimeUtc;
                dstFile.LastWriteTimeUtc = fileDesc.ModifiedTimeUtc;
            }
        }

        private static T GetPrivateProperty<T>(this PackageExtractor extractor, string name)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = extractor.GetType();
            FieldInfo field = type.GetField(name, flags);
            if (field != null)
            {
                return (T)field.GetValue(extractor);
            }

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null)
            {
                return (T)property.GetValue(extractor, null);
            }

            return default(T);
        }

        private static PackageDefinition GetPackageDefinition(PackageExtractor extractor)
        {
            PackageDefinition pkgDef = GetPrivateProperty<PackageDefinition>(extractor, "Package");
            if (pkgDef == null)
            {
                throw new Exception("Unable to get the Package property from PackageExtractor");
            }

            return pkgDef;
        }

        /// <summary>
        /// Remove the leading "/" or "\"
        /// This is the bug identified in PackageExtractor
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetRelativeFilePath(string path)
        {
            if (path.StartsWith(@"\") ||
                path.StartsWith("/"))
            {
                return path.Substring(1);
            }

            return path;
        }

        private static string GetFullPath(DirectoryInfo dir, string relPath)
        {
            var baseUri = new Uri(dir.FullName);
            var targetUri = new Uri(baseUri.AbsoluteUri + "/" + relPath);

            return targetUri.LocalPath;
        }
    }
}
