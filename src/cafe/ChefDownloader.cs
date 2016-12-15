﻿using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace cafe
{
    public class ChefDownloader
    {
        private static ILogger Logger { get; } =
            ApplicationLogging.CreateLogger<ChefDownloader>();

        public const string StagingDirectory = "staging";

        private readonly IFileDownloader _fileDownloader;
        private readonly IFileSystem _fileSystem;

        public ChefDownloader(IFileDownloader fileDownloader, IFileSystem fileSystem)
        {
            _fileDownloader = fileDownloader;
            _fileSystem = fileSystem;
        }

        public void Download(string version)
        {
            _fileSystem.EnsureDirectoryExists(StagingDirectory);
            var downloadLink = DownloadUriFor(version);
            var file = FilenameFor(version);
            bool downloaded = _fileDownloader.Download(downloadLink, Path.Combine(StagingDirectory, file));
            var message = downloaded
                ? $"Chef installer for {version} downloaded at {file}"
                : $"A chef installer for {version} could not be found at link {downloadLink}";
            Logger.LogInformation(message);
        }


        public static Uri DownloadUriFor(string version)
        {
            // TODO: sanitize data so it can't be injected here
            return new Uri($"https://packages.chef.io/files/stable/chef/{version}/windows/2012r2/{FilenameFor(version)}");
        }

        public static string FilenameFor(string version)
        {
            // TODO: sanitize data so it can't be injected here
            return $@"chef-client-{version}-1-x64.msi";
        }
    }
}