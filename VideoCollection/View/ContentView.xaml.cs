﻿using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VideoCollection.Model;
using Converter;
namespace VideoCollection.View
{
    public partial class ContentView : UserControl
    {
        private bool IsPaused = true;
        bool? dialogOk;
        OpenFileDialog fileDialog;
        readonly DispatcherTimer timer;
        private List<VideoDataTemplete> videoDataTempleteList = new List<VideoDataTemplete>();
        private readonly IListViewIndex IListViewIndex = new ListViewIndex();
        public ContentView()
        {
            DataContext = this;
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            timer.Tick += new EventHandler(Timer_tick);
            InitializeComponent();
        }

        private bool SearchFilter(object item)
        {
            if (String.IsNullOrEmpty(SearchTextBox.Text))
                return true;
            else
                return ((item as VideoDataTemplete).VideoName.IndexOf(SearchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        void Timer_tick(object sender, EventArgs e)
        {
            TimeBarSlider.Value = VideoOutput.Position.TotalSeconds;
        }
        public async void OpenFileSystemAsync()
        {
            await Task.Run(() => OpenFileSystem()); //вызов диалогового окна асинхронно
            if(dialogOk == true)
            {
                FillingList(); //заполнение листа объектов
            }
        }
        private void OpenFileSystem()
        {
            fileDialog = new OpenFileDialog
            {
                Multiselect = true
            };
            fileDialog.Filter = "Video (.mp4)|*.mp4"; //фильтр
            dialogOk = fileDialog.ShowDialog();
        }
        private void FillingList()
        {
            FileSizeConverter fileSizeConverter = new FileSizeConverter();
            foreach (string sFileName in fileDialog.FileNames)
            {
                videoDataTempleteList.Add(new VideoDataTemplete()
                {
                    Directory = sFileName,
                    VideoName = RemoveSlash(sFileName),
                    Size = fileSizeConverter.FileSizeConvert(new FileInfo(sFileName).Length),
                    CreationTime = new FileInfo(sFileName).CreationTime
                });

            }
            FillingListView();
        }
        private string RemoveSlash(string value)
        {
            char[] pathCharArray;
            string name = "";
            List<char> pathCharList = new List<char>();
            pathCharArray = value.ToCharArray();
            pathCharArray.Reverse();
            for (int i = value.Length - 1; i > 0; i--)
            {
                if(pathCharArray[i] != '\\')
                {
                    pathCharList.Add(pathCharArray[i]);
                } else
                {
                    pathCharList.Reverse();
                    for(int j = 0; j < pathCharList.Count; j++)
                    {
                        name += pathCharList[j];
                    }
                    break;
                }
            }
            return name;
        }

        private void DataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateInfo();
        }
        private void UpdateInfo()
        {
            try
            {
                AddVideoInMediaElement(new Uri(videoDataTempleteList[this.DataListView.SelectedIndex].Directory));
                TitleTextBox.DataContext = new VideoDataTemplete()
                {
                    VideoName = videoDataTempleteList[this.DataListView.SelectedIndex].VideoName
                };
                SizeTextBox.DataContext = new VideoDataTemplete()
                {
                    Size = videoDataTempleteList[this.DataListView.SelectedIndex].Size
                };
                DateTextBox.DataContext = new VideoDataTemplete()
                {
                    CreationTime = videoDataTempleteList[this.DataListView.SelectedIndex].CreationTime
                };
                LinkTextBox.DataContext = new VideoDataTemplete()
                {
                    Directory = videoDataTempleteList[this.DataListView.SelectedIndex].Directory
                };
                CommentTextBox.DataContext = new VideoDataTemplete()
                {
                    Comment = videoDataTempleteList[this.DataListView.SelectedIndex].Comment
                };
                TagListView.ItemsSource = videoDataTempleteList[this.DataListView.SelectedIndex].Tags;
                VideoOutput.Stop();
                IsPaused = true;
                StateIcon.Kind = PackIconKind.Play;
            }
            catch
            {

            }
        }
        private void AddVideoInMediaElement(Uri value)
        {
            VolumeCheck();
            VideoOutput.DataContext = new VideoDataTemplete()
            {
                SourcePath = value
            };
        }
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoOutput.Volume = (double)(VolumeSlider.Value / 10);
            if (VideoOutput.Volume >= 0.9)
            {
                VolumeIcon.Kind = PackIconKind.VolumeHigh;
                return;
            }
            if (VideoOutput.Volume >= 0.5 && VideoOutput.Volume <= 0.9)
            {
                VolumeIcon.Kind = PackIconKind.VolumeMedium;
                return;
            }
            if (VideoOutput.Volume > 0 && VideoOutput.Volume <= 0.5)
            {
                VolumeIcon.Kind = PackIconKind.VolumeLow;
                return;
            }
            if (VideoOutput.Volume == 0)
            {
                VolumeIcon.Kind = PackIconKind.VolumeMute;
                return;
            }
        }
        private void TimeBarSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoOutput.Position = TimeSpan.FromSeconds(TimeBarSlider.Value);
        }

        private void VideoOutput_MediaOpened(object sender, RoutedEventArgs e)
        {
            TimeSpan time = VideoOutput.NaturalDuration.TimeSpan;
            TimeBarSlider.Maximum = time.TotalSeconds;
            timer.Start();
        }

        private void VideoOutput_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsPaused)
            {
                IsPaused = false;
                VideoOutput.Play();
                StateIcon.Kind = PackIconKind.Pause;
            }
            else
            {
                IsPaused = true;
                VideoOutput.Pause();
                StateIcon.Kind = PackIconKind.Play;
            }
        }
        private void VolumeCheck()
        {
            VideoOutput.Volume = (double)(VolumeSlider.Value / 10);
        }

        private void VideoOutput_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (DataListView.Items.Count > DataListView.SelectedIndex+1)
            {
                DataListView.SelectedIndex += 1;
                AddVideoInMediaElement(new Uri(videoDataTempleteList[this.DataListView.SelectedIndex].Directory));
                IsPaused = false;
                VideoOutput.Play();
                StateIcon.Kind = PackIconKind.Pause;
            }
            else
            {
                VideoOutput.Stop();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileSystemAsync();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            FillingJsonFile();
            Application.Current.Shutdown();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ReadFromJsonOrCreateNewJsonFile();
        }

        private void FillingJsonFile()
        {
            string jsonDataString = "[";
            for (int i = 0; i < videoDataTempleteList.Count; i++)
            {
                if (i < videoDataTempleteList.Count - 1)
                {
                    jsonDataString += (JsonConvert.SerializeObject(videoDataTempleteList[i]).ToString() + ",");
                }
                else
                {
                    jsonDataString += JsonConvert.SerializeObject(videoDataTempleteList[i]).ToString();
                }
            }
            jsonDataString += "]";
            File.WriteAllText("videos.json", jsonDataString);
        }

        private void ReadFromJsonOrCreateNewJsonFile()
        {
            var videosList = File.Exists("videos.json");
            if (videosList)
            {
                var jsonData = JsonConvert.DeserializeObject<List<VideoDataTemplete>>(File.ReadAllText("videos.json"));
                if (jsonData != null)
                {
                    for (int i = 0; i < jsonData.Count; i++)
                    {
                        if(jsonData[i].Tags == null)
                        {
                            jsonData[i].Tags = new List<TagTemplate>();

                        }
                        FillingVideoDataTempleteListFromJson(jsonData[i].Directory, jsonData[i].VideoName, jsonData[i].Size, jsonData[i].CreationTime, jsonData[i].Tags, jsonData[i].Comment);
                    }
                    FillingListView();
                }
            }
            else
            {
                string path = System.IO.Path.Combine(Environment.CurrentDirectory, "videos.json");
                File.Create(path);
            }

        }
        private void FillingVideoDataTempleteListFromJson(string directory, string videoName, string size, DateTime creationTime, List<TagTemplate> tagTemplates, string comment) //заполнение listview из json
        {
            videoDataTempleteList.Add(new VideoDataTemplete()
            {
                Directory = directory,
                VideoName = videoName,
                Size = size,
                Tags = tagTemplates,
                CreationTime = creationTime,
                Comment = comment
            });
        }
        private void FillingListView()
        {
            DataListView.ItemsSource = videoDataTempleteList;
            CollectionViewSource.GetDefaultView(DataListView.ItemsSource).Refresh();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            videoDataTempleteList.RemoveAt(IListViewIndex.GetHoverIndex(DataListView));
            CollectionViewSource.GetDefaultView(DataListView.ItemsSource).Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(DataListView.ItemsSource);
            view.Filter = SearchFilter;
            CollectionViewSource.GetDefaultView(DataListView.ItemsSource).Refresh(); //обновление
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            EditWindow editWindow = new EditWindow(videoDataTempleteList[IListViewIndex.GetHoverIndex(DataListView)], IListViewIndex.GetHoverIndex(DataListView));
            editWindow.ShowDialog();
            if (editWindow.NameVideo != null)
            {
                videoDataTempleteList[editWindow.Index].VideoName = editWindow.NameVideo;
            }
            if(editWindow.TagList.Count > 0)
            {
                videoDataTempleteList[editWindow.Index].Tags = new List<TagTemplate>();
                for (int i = 0; i < editWindow.TagList.Count; i++)
                {
                    videoDataTempleteList[editWindow.Index].Tags.Add(editWindow.TagList[i]);
                }
            }
            videoDataTempleteList[editWindow.Index].Comment = editWindow.CommentVideo;
            FillingJsonFile(); //перезаписать json
            FillingListView();
            UpdateInfo();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            AddYoutubeLinkWindow addYoutubeLinkWindow = new AddYoutubeLinkWindow();
            addYoutubeLinkWindow.ShowDialog();
            if(addYoutubeLinkWindow.GetVideoUri() != null)
            {
                videoDataTempleteList.Add(new VideoDataTemplete()
                {
                    Directory = addYoutubeLinkWindow.GetVideoUri().ToString(),
                    VideoName = addYoutubeLinkWindow.GetName()
                });
                FillingListView();
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            DownloadYoutubeVideoWindow download = new DownloadYoutubeVideoWindow();
            download.ShowDialog();
        }
    }
}