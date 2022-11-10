// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubLocalizeWithDefault<T> : ILocalizeWithDefault<T>
{

    private readonly bool _throwOnLocalizeKeyIsNull;

    /// <summary>
    /// By default it will throw a exception if the localizeKey is null.
    /// The localizeKey can be null in the (rare) cases where you want the
    /// original message to be used without localization
    /// </summary>
    /// <param name="throwOnLocalizeKeyIsNull"></param>
    public StubLocalizeWithDefault(bool throwOnLocalizeKeyIsNull = true)
    {
        _throwOnLocalizeKeyIsNull = throwOnLocalizeKeyIsNull;
    }

    public string LocalizeStringMessage(string localizeKey, string cultureOfMessage, string message)
    {
        if (localizeKey == null && _throwOnLocalizeKeyIsNull)
            throw new ArgumentNullException(nameof(localizeKey));

        SaveLocalizationToDb(localizeKey, cultureOfMessage, message, null);
        return message;
    }

    public string LocalizeFormattedMessage(string localizeKey, string cultureOfMessage,
        params FormattableString[] formattableStrings)
    {
        if (localizeKey == null && _throwOnLocalizeKeyIsNull)
            throw new ArgumentNullException(nameof(localizeKey));

        var message = string.Join(string.Empty, formattableStrings.Select(x => x.ToString()).ToArray());
        var messageFormat = string.Join(string.Empty, formattableStrings.SelectMany(x => x.Format).ToArray());
        SaveLocalizationToDb(localizeKey, cultureOfMessage, message, messageFormat);

        return string.Join(string.Empty, formattableStrings.Select(x => x.ToString()).ToArray());
    }

    private void SaveLocalizationToDb(string localizeKey, string cultureOfMessage, string actualMessage, string messageFormat)
    {
        using var context = GetLocalizationCaptureDbInstance();
        if (context == null)
            return;

        var sameLocalizeKey = context.LocalizedData
            .Where(x => x.LocalizeKey == localizeKey).ToList();
        if (sameLocalizeKey.Any(x => x.MessageFormat == messageFormat))
            //already in the database, so don't add again
            return;

        if (sameLocalizeKey.Any(x => x.LocalizeKey == localizeKey && x.SameKeyButDiffFormat))
            //already has the SameKeyButDiffFormat issue, so don't add again
            return;

        context.Add(new LocalizedData(typeof(T).FullName, localizeKey, cultureOfMessage, 
            actualMessage, messageFormat,
            sameLocalizeKey.Any()));
        context.SaveChanges();
    }

    //-----------------------------------------------------------
    //database part

    public LocalizationCaptureDb GetLocalizationCaptureDbInstance(bool turnOnManually = false)
    {
        var config = AppSettings.GetConfiguration();
        if (!turnOnManually && config["SaveLocalizesToDb"] != "True")
            return null;

        var connectionString = config.GetConnectionString("LocalizationCaptureDb");
        if (connectionString == null)
            throw new Exception("The ConnectionString: 'LocalizationCaptureDd' must be added to make this work");

        var optionsBuilder =
            new DbContextOptionsBuilder<LocalizationCaptureDb>();
        optionsBuilder.UseSqlServer(connectionString);

        var context = new LocalizationCaptureDb(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    public void WipeLocalizationCaptureDb()
    {
        using var context = GetLocalizationCaptureDbInstance(true);
        if (context == null) 
            return;

        if (context.Database.EnsureCreated()) 
            return;
        //The database exists so wipe the entries
        context.RemoveRange(context.LocalizedData.ToList());
        context.SaveChanges();
    }

    public List<LocalizedData> ListLocalizationCaptureDb()
    {
        using var context = GetLocalizationCaptureDbInstance(true);
        if (context == null)
            return null;

        return context.LocalizedData.OrderBy(l => l.ClassFullName).ThenBy(l => l.LocalizeKey)
            .ToList();
    }

    public class LocalizedData
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public LocalizedData(string classFullName, string localizeKey, string cultureOfMessage, 
            string actualMessage, string messageFormat, bool sameKeyButDiffFormat)
        {
            ClassFullName = classFullName;
            LocalizeKey = localizeKey;
            CultureOfMessage = cultureOfMessage;
            ActualMessage = actualMessage;
            MessageFormat = messageFormat;
            SameKeyButDiffFormat = sameKeyButDiffFormat;
        }

        public int Id { get; set; }
        public string ClassFullName { get; set; }
        public string LocalizeKey { get; set; }
        public string CultureOfMessage { get; set; }
        public string ActualMessage { get; set; }
        public string MessageFormat { get; set; }
        public bool SameKeyButDiffFormat { get; set;}
    }

    public class LocalizationCaptureDb : DbContext
    {
        public LocalizationCaptureDb(DbContextOptions<LocalizationCaptureDb> options)
            : base(options) {}

        public DbSet<LocalizedData> LocalizedData { get; set; }
    }
}