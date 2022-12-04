// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubLocalizeWithDefaultWithLogging<TResource> : ILocalizeWithDefault<TResource>
{
    /// <summary>
    /// This is true if there is an entry in the database with the same Resource/Key, but a different.
    /// Used in the test of this class.
    /// </summary>
    public bool SameKeyButDiffFormat { get; set; }

    public string LocalizeStringMessage(LocalizeKeyData localizeKeyData, string cultureOfMessage, string message)
    {
        if (localizeKeyData == null)
            throw new ArgumentNullException(nameof(localizeKeyData));

        SaveLocalizationToDb(localizeKeyData, cultureOfMessage, message, null);
        return message;
    }

    public string LocalizeFormattedMessage(LocalizeKeyData localizeKeyData, string cultureOfMessage,
        params FormattableString[] formattableStrings)
    {
        if (localizeKeyData == null)
            throw new ArgumentNullException(nameof(localizeKeyData));

        var message = string.Join(string.Empty, formattableStrings.Select(x => x.ToString()).ToArray());
        var messageFormat = string.Join(string.Empty, formattableStrings.SelectMany(x => x.Format).ToArray());
        SaveLocalizationToDb(localizeKeyData, cultureOfMessage, message, messageFormat);

        return string.Join(string.Empty, formattableStrings.Select(x => x.ToString()).ToArray());
    }

    /// <summary>
    /// This adds  information on each localized message, including where it was sent from,
    /// so that you can see what localized messages in your app. Usually you would use the
    /// <see cref="StubLocalizeWithDefaultWithLogging{TResource}"/> within your unit tests.
    /// It tries to: 
    /// 1) Add a new entry in the database if there isn't an entry containing the same information.
    /// 2) It also sets the <see cref="LocalizedLog"/>.<see cref="LocalizedLog.SameKeyButDiffFormat"/> to true
    /// if an existing entry with the same ResourceFile / LocalizeKey, but a different different message.
    /// </summary>
    /// <param name="localizeKeyData"></param>
    /// <param name="cultureOfMessage"></param>
    /// <param name="actualMessage"></param>
    /// <param name="messageFormat"></param>
    private void SaveLocalizationToDb(LocalizeKeyData localizeKeyData, string cultureOfMessage, string actualMessage,
        string messageFormat)
    {
        using var context = GetLocalizationCaptureDbInstance();
        if (context == null)
            return;

        var localizeKey = localizeKeyData.LocalizeKey ?? "already localize";
        var callingClassName = GetFormattedName(localizeKeyData.CallingClass);

        //This will hold any existing database entries that have the same ResourceFile and LocalizeKey
        var sameLocalizationReference = context.LocalizedData
            .Where(x => x.ResourceClassType == typeof(TResource).FullName
                        && x.LocalizeKey == localizeKey).ToList();

        //This is true if there was already an entry with the same ResourceFile / LocalizeKey but a different message
        SameKeyButDiffFormat = sameLocalizationReference.Any(x =>
            x.ResourceClassType == typeof(TResource).FullName
            && x.LocalizeKey == localizeKey
            && ((x.MessageFormat != null && x.MessageFormat != messageFormat)
                || (x.MessageFormat == null && x.ActualMessage != actualMessage)));

        var localizedLog = new LocalizedLog(typeof(TResource).FullName, localizeKey,
            cultureOfMessage, actualMessage, messageFormat, SameKeyButDiffFormat,
            callingClassName, localizeKeyData.MethodName, localizeKeyData.SourceLineNumber);

        if (sameLocalizationReference.Any(x =>
                ((x.MessageFormat != null && x.MessageFormat == messageFormat)     //Using format: and the same
                || (x.MessageFormat == null && x.ActualMessage == actualMessage))  //Using string: and the same
                && x.SameKeyButDiffFormat == SameKeyButDiffFormat
                && x.CallingClassName == localizedLog.CallingClassName
                && x.CallingMethodName == localizedLog.CallingMethodName
                && x.SourceLineNumber == localizedLog.SourceLineNumber))
            //There is already an entry with the same 
            return;

        context.Add(new LocalizedLog(typeof(TResource).FullName, localizeKey, 
            cultureOfMessage, actualMessage, messageFormat, SameKeyButDiffFormat,
            callingClassName, localizeKeyData.MethodName, localizeKeyData.SourceLineNumber));
        context.SaveChanges();
    }

    ///thanks to https://stackoverflow.com/a/66604069/1434764
    /// <summary>
    /// Returns the type name. If this is a generic type, appends
    /// the list of generic type arguments between angle brackets.
    /// (Does not account for embedded / inner generic arguments.)
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>System.String.</returns>
    public static string GetFormattedName(Type type)
    {
        if (type.IsGenericType)
        {
            string genericArguments = type.GetGenericArguments()
                .Select(x => x.Name)
                .Aggregate((x1, x2) => $"{x1}, {x2}");
            return $"{type.Name.Substring(0, type.Name.IndexOf("`"))}"
                   + $"<{genericArguments}>";
        }
        return type.Name;
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
            throw new Exception("The ConnectionString: 'LocalizationCaptureDd' must be added to the appsettings file to make this work.");

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

    public List<LocalizedLog> ListLocalizationCaptureDb()
    {
        using var context = GetLocalizationCaptureDbInstance(true);
        if (context == null)
            return null;

        return context.LocalizedData.OrderBy(l => l.ResourceClassType).ThenBy(l => l.LocalizeKey)
            .ToList();
    }

    public class LocalizedLog
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public LocalizedLog(string resourceClassType, string localizeKey, string cultureOfMessage, 
            string actualMessage, string messageFormat, bool sameKeyButDiffFormat, string callingClassName, string callingMethodName, int sourceLineNumber)
        {
            ResourceClassType = resourceClassType;
            LocalizeKey = localizeKey;
            CultureOfMessage = cultureOfMessage;
            ActualMessage = actualMessage;
            MessageFormat = messageFormat;
            SameKeyButDiffFormat = sameKeyButDiffFormat;
            CallingClassName = callingClassName;
            CallingMethodName = callingMethodName;
            SourceLineNumber = sourceLineNumber;
        }

        public int Id { get; set; }
        public string ResourceClassType { get; set; }
        public string LocalizeKey { get; set; }
        public string CultureOfMessage { get; set; }
        public string ActualMessage { get; set; }
        public string MessageFormat { get; set; }
        public bool SameKeyButDiffFormat { get; set;}
        public string CallingClassName { get; set; }
        public string CallingMethodName { get; set; }
        public int SourceLineNumber { get; set; }
    }

    public class LocalizationCaptureDb : DbContext
    {
        public LocalizationCaptureDb(DbContextOptions<LocalizationCaptureDb> options)
            : base(options) {}

        public DbSet<LocalizedLog> LocalizedData { get; set; }
    }
}