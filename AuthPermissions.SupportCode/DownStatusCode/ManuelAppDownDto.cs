// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.SupportCode.DownStatusCode;

/// <summary>
/// This is used in the "app down" and contains information to show to users
/// while the application is "down for maintenance" 
/// </summary>
public class ManuelAppDownDto
{
    /// <summary>
    /// Id of the user that set the "all down" status
    /// This allows that user to still use the app
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Optional message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Optional: This contains the expected time the app will be not available to users
    /// </summary>
    public int? ExpectedTimeDownMinutes { get; set; }

    /// <summary>
    /// Set this to the start time 
    /// </summary>
    public DateTime StartedUtc { get; set; }
}