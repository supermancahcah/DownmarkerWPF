using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using MarkPad.DocumentSources.MetaWeblog;
using MarkPad.DocumentSources.WebSources;
using MarkPad.Infrastructure;
using MarkPad.Infrastructure.DialogService;
using MarkPad.Plugins;
using MarkPad.Settings.Models;
using Ookii.Dialogs.Wpf;

namespace MarkPad.DocumentSources.GitHub
{
    public class WebDocument : MarkpadDocumentBase
    {
        readonly BlogSetting blog;
        readonly IWebDocumentService webDocumentService;
        readonly List<string> categories = new List<string>();

        public WebDocument(
            BlogSetting blog,
            string id,
            string title,
            string content,
            IEnumerable<FileReference> associatedFiles,
            IDocumentFactory documentFactory,
            IWebDocumentService webDocumentService,
            WebSiteContext siteContext,
            IFileSystem fileSystem) :
            base(title, content, blog.BlogName, associatedFiles, documentFactory, siteContext, fileSystem)
        {
            Id = id;
            this.blog = blog;
            this.webDocumentService = webDocumentService;
        }

        public string Id { get; private set; }

        public override async Task<IMarkpadDocument> Save()
        {
            var result = await webDocumentService.SaveDocument(blog, this);

            Id = result.Id;
            if (!string.IsNullOrEmpty(result.NewDocumentContent))
                MarkdownContent = result.NewDocumentContent;

            return this;
        }

        public List<string> Categories
        {
            get { return categories; }
        }

        public override Task<IMarkpadDocument> Publish()
        {
            var save = new ButtonExtras(ButtonType.Yes, "保存", "保存博文到你的博客。");
            var saveAs = new ButtonExtras(ButtonType.No, "另存为", "保存本博客博文为本地 markdown 文件。");
            var publish = new ButtonExtras(ButtonType.Retry, "发布为", "发布本次博文到另外一个博客，或者发布为另外一个博文。");

            var service = new DialogMessageService(null)
            {
                Icon = DialogMessageIcon.Question,
                Buttons = DialogMessageButtons.Yes | DialogMessageButtons.No | DialogMessageButtons.Retry | DialogMessageButtons.Cancel,
                Title = "Markpad",
                Text = string.Format("{0} 已经发布了，你想做什么？", Title),
                ButtonExtras = new[] { save, saveAs, publish }
            };

            var result = service.Show();
            switch (result)
            {
                case DialogMessageResult.Yes:
                    return Save();
                case DialogMessageResult.No:
                    return SaveAs();
                case DialogMessageResult.Retry:
                    return DocumentFactory.PublishDocument(null, this);
            }

            return TaskEx.FromResult<IMarkpadDocument>(this);
        }

        public override FileReference SaveImage(Bitmap image)
        {
            var workingDirectory = SiteContext.WorkingDirectory;
            var imageFileName = GetFileNameBasedOnTitle(Title, workingDirectory);

            image.Save(Path.Combine(workingDirectory, imageFileName), ImageFormat.Png);

            var relativePath = ToRelativePath(workingDirectory, workingDirectory, imageFileName);
            var fileReference = new FileReference(imageFileName, relativePath, false);
            AddFile(fileReference);

            return fileReference;
        }

        public override string ConvertToAbsolutePaths(string htmlDocument)
        {
            return ConvertToAbsolutePaths(htmlDocument, SiteContext.WorkingDirectory);
        }

        public override bool IsSameItem(ISiteItem siteItem)
        {
            var webDocumentItem = siteItem as WebDocumentItem;
            if (webDocumentItem != null)
                return webDocumentItem.Id == Id;

            return false;
        }
    }
}