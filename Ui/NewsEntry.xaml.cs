using System.Windows.Controls;
using GTANews;

namespace GTAVModdingLauncher.Ui
{
	public partial class NewsEntry : UserControl
	{
		public NewsEntry(NewsBox parent, News news)
		{
			this.InitializeComponent();

			this.Headline.Text = news.Headline;
			this.Subtitle.Text = news.Subtitle;
			this.Content.Text = news.Content;
			parent.ImgLoader.LoadAsync(news.Id, this.Image, news.Image);
		}
	}
}
