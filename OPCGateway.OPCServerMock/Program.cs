using OPCGateway.OPCServerMock;

namespace OPCGateway.OPCServerMock;

public static class Program
{
    public static int Main(string[] args)
    {
        MockOpcServer? mockOpcServer = null;

        try
        {
            mockOpcServer = new MockOpcServer();
            mockOpcServer.StartAsync();

            Console.WriteLine("Both servers are running. Press Enter to exit.");
            Console.WriteLine("WARNING: Running without security - use only in development/testing environments!");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ReadLine();
            return 1;
        }
        finally
        {
            // Ensure both mock servers are stopped even on exceptions.
            mockOpcServer?.Stop();
        }
    }
}