namespace _Project.Gameplay.Features.PooledView
{
    public interface IViewPoolCallbacks
    {
        void OnGet();
        void OnRelease();
    }
}
