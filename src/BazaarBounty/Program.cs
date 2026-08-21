using System;

namespace BazaarBounty
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = BazaarBountyGame.GetGameInstance())
                game.Run();
        }
    }
}
