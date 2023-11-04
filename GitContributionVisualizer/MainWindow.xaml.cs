using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibGit2Sharp;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace GitContributionVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static string PickFolderPath()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }
            return null;
        }

        private async void PickRepoButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ProgressBar.Visibility = Visibility.Visible;

                string repoPath = PickFolderPath();
                if (repoPath != null)
                {
                    this.PathOutput.Text = repoPath;
                    var repo = new Repository(repoPath);

                    var gitData = await GetGitDataByFolderFromRepository(repo);
                    var html = await GetHtmlFromGitDataByFolder(gitData);
                    this.WebView2.NavigateToString(html);
                }
            }
            finally
            {
                this.ProgressBar.Visibility = Visibility.Hidden;   
            }
        }

        public class GitDataByFolder
        {
            private string NormalizePath(string nonNormalPath) {
                return nonNormalPath.Replace("\\", "/").TrimStart('/');
            }

            private string NormalizePathToFolderPath(string nonNormalPath)
            {
                string normalPath = NormalizePath(nonNormalPath);
                if (normalPath.Length > 0)
                {
                    // Remove everything after the last /    
                    int lastSlashIndex = normalPath.LastIndexOf('/');
                    if (lastSlashIndex != -1)
                    {
                        normalPath = normalPath.Substring(0, lastSlashIndex);
                    }
                }
                return normalPath;
            }
            
            public GitDataByFolder(Repository repository, GitDataByFolder root = null, GitDataByFolder parent = null, string name = "")
            {
                Repository = repository;
                Root = root == null ? this : root;
                Parent = parent == null ? this : parent;
                Name = name;
            }

            public GitDataByFolder GetOrCreateChild(string nonNormalPath)
            {
                string path = NormalizePathToFolderPath(nonNormalPath);
                string[] pathParts = path.Split("/");
                return GetOrCreateChild(pathParts);
            }
            public GitDataByFolder GetOrCreateChild(string[] pathParts, int startIdx = 0)
            {
                Debug.Assert(startIdx < pathParts.Length || pathParts.Length == 0);

                if (startIdx < pathParts.Length)
                {
                    GitDataByFolder child = Children.FirstOrDefault(child => child.Name == pathParts[startIdx], null);
                    if (child == null)
                    {
                        child = new GitDataByFolder(this.Repository, this.Root, this.Parent, pathParts[startIdx]);
                    }
                    return child.GetOrCreateChild(pathParts, startIdx + 1);
                }
                else
                {
                    return this;
                }
            }
            public void AddCommit(Commit commit)
            {
                string key = commit.Author.Email;
                if (!this.AuthorToCount.ContainsKey(key))
                {
                    this.AuthorToCount[key] = 1;
                }
                else
                {
                    ++this.AuthorToCount[key];
                }
            }
            public Repository Repository { get; private set; }
            public GitDataByFolder Root { get; private set; }
            public GitDataByFolder Parent { get; private set; }
            public string Name { get; private set; }
            public List<GitDataByFolder> Children { get; } = new List<GitDataByFolder>();
            public Dictionary<string, int> AuthorToCount { get; } = new Dictionary<string, int>();
        }

        private async Task<GitDataByFolder> GetGitDataByFolderFromRepository(Repository repo)
        {
            GitDataByFolder root = new GitDataByFolder(repo, null, null, "");
            foreach (Commit commit in repo.Commits)
            {
                foreach (var treeEntry in commit.Tree)
                {
                    var path = treeEntry.Path;
                    root.GetOrCreateChild(path).AddCommit(commit);
                }
            }
            return root;
        }


        private async Task<string> GetHtmlFromGitDataByFolder(GitDataByFolder data)
        {
            return "<h1>Example</h1>";
        }
    }
}