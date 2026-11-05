using System.Windows;

// ThemeInfo 告诉 WPF 去哪里找主题资源字典。
// 本项目的样式都放在 App.xaml，没有单独的 Themes 目录，所以两个位置都用默认设置。
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            // 主题专用资源字典的位置（没有）
    ResourceDictionaryLocation.SourceAssembly   // 通用资源字典随程序集存放
)]
