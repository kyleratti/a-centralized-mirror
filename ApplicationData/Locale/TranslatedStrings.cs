namespace ApplicationData.Locale;

public static class TranslatedStrings
{
	public static class LinkController
	{
		public const string LinkNotFound = @"A link with this URL for the specified reddit post ID does not exist.";
		public const string LinkAlreadyExists = @"A link of this type with this URL for the specified reddit post ID already exists.";
		public const string LinkDeleted = @"This link has been deleted successfully. The available links on reddit will be updated shortly.";
		public static string LinkIdNotFound(int linkId) => $"A link with this ID does not exist or does not belong to you: {linkId}";

		public static string RedditPostIdIsNotValid(string redditPostId) =>
			$"The specified Reddit Post ID '{redditPostId}' is not valid. Either the submission does not exist, is in a private community, is locked from new replies, or the submission has been archived.";
	}
}