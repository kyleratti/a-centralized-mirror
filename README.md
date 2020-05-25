# a-centralized-mirror

Typically, subreddits want mirror links of submissions to be stickied to the top of each thread so viewers can find working mirrors without having to filter through comments themselves.

The **a-centralized-mirror** service maintains a single stickied comment with a list of links to each mirror. Sticky replies are locked to prevent users from using its sticky status to make their comments appear at the top. Registered bots can post, update, or delete their links with this service at any time; the **a-mirror-bot** account will update the stickied comment accordingly. Here's a general overview of how it works:

1. **handsome-third-party-mirror-bot** downloads a video to mirror
2. **handsome-third-party-mirror-bot** submits an API request to this service with the link to the video
3. **a-mirror-bot** posts, updates, or deletes the stickied comment on the submission

## Example Posts

A post maintained by **a-mirror-bot** will look similar to the following:

> The following mirrors are available:
>
> - [Mirror #1](https://youtube.com/) (provided by /u/a-mirror-bot)
> - [Mirror #2](https://youtube.com/) (provided by /u/not-a-mirror-bot)
>
> **Note:** this is a bot providing a directory service. **If you have trouble with any of the links above, please contact the user who provided them.**
>
> ---
>
> [^(source code)](https://amirror.link/source) ^| [^(run your own mirror bot? let's integrate)](https://amirror.link/lets-talk)

Mirrors are listed in the order in which they're sent to the mirror service.

# Subreddit Moderators

Although this bot can function on a subreddit with standard access/without moderator privileges, it works in a much more limited capacity.

Out of respect for subreddit moderators, this bot will only be activated on subreddits where a discussion has been had and an "agreement" is in place.

Additionally, any new bots are subject to review by subreddit moderators before being added to this service.

If you're a moderator of a subreddit and your community is interested in having your mirror bots use a directory listing of mirrors, please [open an issue](https://github.com/kyleratti/a-centralized-mirror/issues/new?assignees=kyleratti&labels=subreddit+partnership&template=subreddit-partnership.md&title=).

If you're looking for a new or additional mirror _bot_ and not a mirror directory listing _service_, please see the [**Tuckbot**](https://github.com/kyleratti/tuckbot-downloader) project.

# Bot Developers

If you're interested in integrating your bot with this service, please take a moment to read over [our wiki](https://github.com/kyleratti/a-centralized-mirror/wiki). You will find additional information and an overview of our API.

If you've read those resources and would like to integrate, please see the [Add Your Bot](https://github.com/kyleratti/a-centralized-mirror/wiki/Add-Your-Bot) page of the wiki for details on how to do that.

# Implementation

If you are integrating **a-centralized-mirror** into your mirror service, it's _highly_ recommended that you add error handling for cases where the service may be unavailable or there is an error in processing your request. Each endpoint will try to return as descriptive errors as possible, including HTTP status codes and a JSON response.

Consider **queuing and retrying requests** occasionally until they succeed or **bypassing the service entirely and manually posting the mirror link** using your own bot.

This service is provided as a fun hobby and does not come with any SLA.

# And a standing ovation to...

I absolutely _cannot_ thank **[RoboPhred](https://github.com/robophred)** enough for this help with this project and every single project I've worked on for the past decade. You are, without a doubt, the smartest man I'll ever know and the only reason I can program at all. Thank you so very much for your patience over the past _decade_ of mentorship you've provided me.
