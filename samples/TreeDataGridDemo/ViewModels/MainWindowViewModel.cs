namespace TreeDataGridDemo.ViewModels
{
    internal class MainWindowViewModel
    {
        public CountriesPageViewModel Countries => field ??= new CountriesPageViewModel();

        public FilesPageViewModel Files => field ??= new FilesPageViewModel();

        public WikipediaPageViewModel Wikipedia => field ??= new WikipediaPageViewModel();

        public DragDropPageViewModel DragDrop => field ??= new DragDropPageViewModel();

        public DynamicColumnsViewModel Dynamic => field ??= new DynamicColumnsViewModel();
    }
}
