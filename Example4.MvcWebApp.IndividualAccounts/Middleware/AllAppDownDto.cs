// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace Example4.MvcWebApp.IndividualAccounts.Middleware;

public class AllAppDownDto
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