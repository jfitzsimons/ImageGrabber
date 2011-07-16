//-----------------------------------------------------------------------------
// ImageGrabber.cs by Jeff Fitzsimons (7/2011)
//-----------------------------------------------------------------------------
// A dirt-simple image downloader.  This is a command-line app which accepts
// one or more URLs as arguments.  It will download each URL, exctract all
// image links, and download each linked image.
//
// A named mutex is used to limit active downloads to one instance at a time.
//
// This application is intended for .Net 2.0 and has been compiled successfully
// with the Mono Project's C# compiler, gmcs.
//-----------------------------------------------------------------------------

// No 'using' statements since I want all calls and declarations to be explicit.

public class ImageGrabber
{
    public static void Main(string[] args)
    {
        // Show usage if we don't have at least one URL argument...
        if (args.Length < 1)
        {
            System.Console.WriteLine("Usage:  ImageGrabber URL");
            System.Console.WriteLine("   Where URL is the page containing image links to retrieve.");
            System.Console.WriteLine("   Multiple URLs may be specified, separated by spaces.");
            return;
        }

        // Create a named (global) mutex so that only one instance at a time is
        // actively downloading...
        bool bMutexCreated = false;
        System.Threading.Mutex mutex = new System.Threading.Mutex(
            true, 
            "ImageGrabber Sychronization Mutex", 
            out bMutexCreated);

        if (!bMutexCreated)
        {
            System.Console.WriteLine("Waiting for previous instance to complete...");
            mutex.WaitOne();

            // Note:  if a thread terminates while it owns a mutex, that mutex
            // is set to the signalled state.  In other words, even if an
            // exception is thrown and this process dies while owning it, other
            // other instances will continue to run.
        }

        // Gather statistics as we go:
        int cDownloaded = 0, cFailed = 0, cSkipped = 0;
        
        // Use a WebClient instance to download files:
        System.Net.WebClient Client = new System.Net.WebClient();
       
        foreach (string url in args)
        {
            System.Console.WriteLine("Retrieving URL \"{0}\"...", url);
            string s = Client.DownloadString(url);  // Req's .Net 2.0.

            // Extract all image URLs:
            System.Text.RegularExpressions.MatchCollection matches = 
                System.Text.RegularExpressions.Regex.Matches(
                    s, 
                    ".*a href=\"([^\"]*.(jpg|png|gif))", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                // Extract the filename from the whole URL...
                int lastSlash = match.Groups[1].Value.LastIndexOf('/');
                if (lastSlash == -1)
                { 
                    // The only way a slash would not be found is if the file was
                    // in the same folder as the source page.  We're just not going
                    // to handle that case.  
                    System.Console.WriteLine("Invalid URL:  {0}",
                            match.Groups[1].Value);
                    continue;
                }

                string filename = match.Groups[1].Value.Substring(lastSlash + 1);

                if (System.IO.File.Exists(filename))
                {
                    System.Console.WriteLine("Skipping existing image {0}",
                        filename);
                    cSkipped += 1;
                    continue;
                }

                System.Console.WriteLine("Downloading image:  {0}",
                    match.Groups[1].Value);

                bool bSucceeded = false;
                int cRetries = 0;
                while (!bSucceeded && cRetries++ < 5)
                {
                    try
                    {
                        Client.DownloadFile(match.Groups[1].Value, filename);
                            
                        cDownloaded += 1;
                        bSucceeded = true;
                    }
                    catch (System.Net.WebException)
                    {
                        System.Console.WriteLine("   Error downloading, retrying ({0})",
                            cRetries);
                    }
                }

                if (bSucceeded == false)
                {
                    System.Console.WriteLine("   Failed.");
                    cFailed += 1;
                }
            }
        }

        System.Console.WriteLine("{0} downloaded, {1} skipped, {2} failed.", 
            cDownloaded, cSkipped, cFailed);
        System.Console.WriteLine("Done.");

        mutex.ReleaseMutex();
    }
}

