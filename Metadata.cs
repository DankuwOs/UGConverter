using System.IO;

namespace UGConverter;

public class Metadata
{
    public Metadata(string relPath, string guid, long fileSize, string hash)
    {
        relativePath = relPath;
        
        this.guid = guid;
        this.fileSize = fileSize;
        this.hash = hash;
    }
    
    public string relativePath;
    public string guid;
    public long fileSize;
    public string hash;

    public bool Matches(Metadata other)
    {
        return relativePath == other.relativePath && fileSize == other.fileSize && hash == other.hash;
    }
}