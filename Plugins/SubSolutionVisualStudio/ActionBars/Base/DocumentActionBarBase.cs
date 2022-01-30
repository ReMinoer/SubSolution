using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;

namespace SubSolutionVisualStudio.ActionBars.Base
{
    public abstract class DocumentActionBarBase : ActionBarBase
    {
        public string DocumentFilePath { get; }
        public DocumentView? DocumentView { get; private set; }

        protected DocumentActionBarBase(string documentFilePath)
        {
            DocumentFilePath = documentFilePath;
        }

        protected override async Task<WindowFrame?> GetWindowFrameAsync(CancellationToken cancellationToken)
        {
            DocumentView = await VS.Documents.GetDocumentViewAsync(DocumentFilePath);
            return DocumentView?.WindowFrame;
        }
    }
}