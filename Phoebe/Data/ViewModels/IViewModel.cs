namespace Toggl.Phoebe.Data.ViewModels
{
    public interface IViewModel<T>
    {
        bool IsLoading { get; }

        void Dispose ();
    }
}

