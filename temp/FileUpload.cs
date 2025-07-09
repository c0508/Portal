using System;
using System.Collections.Generic;

namespace ESGPlatform.temp;

public partial class FileUpload
{
    public int Id { get; set; }

    public int ResponseId { get; set; }

    public string UploadedById { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual Response Response { get; set; } = null!;

    public virtual User UploadedBy { get; set; } = null!;
}
