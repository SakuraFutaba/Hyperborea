using ECommons.Singletons;

namespace Hyperborea.Services;
public static class S
{
    [Priority(int.MaxValue)] public static ThreadPool ThreadPool { get; private set; }
}
