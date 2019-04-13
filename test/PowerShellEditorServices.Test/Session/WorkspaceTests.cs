﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.PowerShell.EditorServices.Utility;
using Xunit;

namespace Microsoft.PowerShell.EditorServices.Test.Session
{
    public class WorkspaceTests
    {
        private static readonly Version PowerShellVersion = new Version("5.0");

        private static readonly Lazy<string> s_lazyDriveLetter = new Lazy<string>(() => Path.GetFullPath("\\").Substring(0, 1));

        public static string CurrentDriveLetter => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? s_lazyDriveLetter.Value
            : string.Empty;

        [Fact]
        public void CanResolveWorkspaceRelativePath()
        {
            string workspacePath = @"c:\Test\Workspace\";
            string testPathInside = @"c:\Test\Workspace\SubFolder\FilePath.ps1";
            string testPathOutside = @"c:\Test\PeerPath\FilePath.ps1";
            string testPathAnotherDrive = @"z:\TryAndFindMe\FilePath.ps1";

            Workspace workspace = new Workspace(PowerShellVersion, Logging.NullLogger);

            // Test without a workspace path
            Assert.Equal(testPathOutside, workspace.GetRelativePath(testPathOutside));

            // Test with a workspace path
            workspace.WorkspacePath = workspacePath;
            Assert.Equal(@"SubFolder\FilePath.ps1", workspace.GetRelativePath(testPathInside));
            Assert.Equal(@"..\PeerPath\FilePath.ps1", workspace.GetRelativePath(testPathOutside));
            Assert.Equal(testPathAnotherDrive, workspace.GetRelativePath(testPathAnotherDrive));
        }

        [Fact]
        public void CanDetermineIsPathInMemory()
        {
            var tempDir = Environment.GetEnvironmentVariable("TEMP");
            var shortDirPath = Path.Combine(tempDir, "GitHub", "PowerShellEditorServices");
            var shortFilePath = Path.Combine(shortDirPath, "foo.ps1");
            var shortUriForm = "git:/c%3A/Users/Keith/GitHub/dahlbyk/posh-git/src/PoshGitTypes.ps1?%7B%22path%22%3A%22c%3A%5C%5CUsers%5C%5CKeith%5C%5CGitHub%5C%5Cdahlbyk%5C%5Cposh-git%5C%5Csrc%5C%5CPoshGitTypes.ps1%22%2C%22ref%22%3A%22~%22%7D";
            var longUriForm = "gitlens-git:c%3A%5CUsers%5CKeith%5CGitHub%5Cdahlbyk%5Cposh-git%5Csrc%5CPoshGitTypes%3Ae0022701.ps1?%7B%22fileName%22%3A%22src%2FPoshGitTypes.ps1%22%2C%22repoPath%22%3A%22c%3A%2FUsers%2FKeith%2FGitHub%2Fdahlbyk%2Fposh-git%22%2C%22sha%22%3A%22e0022701fa12e0bc22d0458673d6443c942b974a%22%7D";

            var testCases = new[] {
                // Test short file absolute paths
                new { IsInMemory = false, Path = shortDirPath },
                new { IsInMemory = false, Path = shortFilePath },
                new { IsInMemory = false, Path = new Uri(shortDirPath).ToString() },
                new { IsInMemory = false, Path = new Uri(shortFilePath).ToString() },

                // Test short file relative paths - not sure we'll ever get these but just in case
                new { IsInMemory = false, Path = "foo.ps1" },
                new { IsInMemory = false, Path = ".." + Path.DirectorySeparatorChar + "foo.ps1" },

                // Test short non-file paths
                new { IsInMemory = true,  Path = "untitled:untitled-1" },
                new { IsInMemory = true,  Path = shortUriForm },
                new { IsInMemory = true, Path = "inmemory://foo.ps1" },

                // Test long non-file path - known to have crashed PSES
                new { IsInMemory = true,  Path = longUriForm },
            };

            foreach (var testCase in testCases)
            {
                Assert.True(
                    Workspace.IsPathInMemory(testCase.Path) == testCase.IsInMemory,
                    $"Testing path {testCase.Path}");
            }
        }

        [Theory()]
        [MemberData(nameof(s_PathsToResolve), parameters: 2)]
        public void CorrectlyResolvesPaths(string givenPath, string expectedPath)
        {
            Workspace workspace = new Workspace(PowerShellVersion, Logging.NullLogger);
            string resolvedPath = workspace.ResolveFilePath(givenPath);
            Assert.Equal(expectedPath, resolvedPath);
        }

        public static readonly object[][] s_PathsToResolve = new object[][]
        {
            new [] { "file:///C%3A/banana/", @"C:\banana\" },
            new [] { "file:///C%3A/banana/ex.ps1", @"C:\banana\ex.ps1" },
            new [] { "file:///E%3A/Path/to/awful%23path", @"E:\Path\to\awful#path" },
            new [] { "file:///path/with/no/drive", $@"{CurrentDriveLetter}:\path\with\no\drive" },
            new [] { "file:///path/wi[th]/squ[are/brackets/", $@"{CurrentDriveLetter}:\path\wi[th]\squ[are\brackets\" },
            new [] { "file:///Carrots/A%5Ere/Good/", $@"{CurrentDriveLetter}:\Carrots\A^re\Good\" },
            new [] { "file:///Users/barnaby/%E8%84%9A%E6%9C%AC/Reduce-Directory", $@"{CurrentDriveLetter}:\Users\barnaby\脚本\Reduce-Directory" },
            new [] { "file:///C%3A/Program%20Files%20%28x86%29/PowerShell/6/pwsh.exe", @"C:\Program Files (x86)\PowerShell\6\pwsh.exe" },
            new [] { "file:///home/maxim/test%20folder/%D0%9F%D0%B0%D0%BF%D0%BA%D0%B0/helloworld.ps1", $@"{CurrentDriveLetter}:\home\maxim\test folder\Папка\helloworld.ps1" }
        };
    }
}
