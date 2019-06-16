# a-centralized-mirror

Typically, subreddits want mirror links stickied to the top of each thread so readers can find live mirrors without having to filter through comments themselves.

The **a-centralized-mirror** service will maintain a single stickied comment with a list of links to the mirrors. Registered bots can update their status (new/updated/removed mirror) on-demand and **a-mirror-bot** will update the stickied comment accordingly. Here's a general overview of how it works:

1. **some-other-mirror-bot** downloads a video to mirror
2. **some-other-mirror-bot** submits an API request to this service
3. **a-mirror-bot** posts/updates the stickied comment on the post

## Example Posts

A post maintained by **a-mirror-bot** will look similar to the following:

> The following mirrors are available:
>
> - [Mirror #1](https://youtube.com/) (provided by /u/a-mirror-bot)
>
> **Note:** this listing is provided for convenience only; if you have issues using one of the mirrors listed here, please address them directly with the bot developer
>
> ---
>
> [^share ^your ^thoughts](https://centralized.amirror.link/thoughts) ^| [^look ^at ^my ^programming](https://centralized.amirror.link/source)

Mirrors are listed in the order in which they're sent to the mirror service.

# Subreddit Moderators

This bot will only be activated on subreddits where the moderators have already approved its functionality and an agreement is in place.

If you're a moderator of a subreddit and your community is interested in having its mirror bots use a single stickied list of mirrors, please [contact the developer](https://reddit.com/message/compose/?to=a-mirror-bot&subject=a-mirror-bot%20-%20new%20subreddit%20support) for additional information.

If you're looking for a new or additional mirror _bot_ and not a mirror listing _service_, please see the [**a-mirror-bot** service](https://amirror.link/source) project.

# Bot Developers

If you're interested in integrating your bot with this service, please take a moment to read over [our wiki](https://centralized.amirror.link/source/wiki). You will find additional information and an overview of our API.

You will need to [contact the developer](https://reddit.com/message/compose/?to=a-mirror-bot&subject=a-mirror-bot%20-%20api%20access) to register your bot and receive your unique API access tokens. In your message, please include:

```
bot username:
developer username:
active subreddits:
```

## Implementation

If you are integrating **a-centralized-mirror** into your mirror service, it's highly recommended that you add error handling for cases where the service may be unavailable or have an error in processing your request. Consider **queuing and retrying requests** occasionally until they succeed or **bypassing the service entirely and manually posting the mirror link** using your own bot.

This service is provided as a fun hobby and does not come with any SLA.

# And a standing ovation to...

I absolutely _cannot_ thank **[RoboPhred](https://github.com/robophred)** enough for this help with this project and every single project I've worked on for the past decade. You are, without a doubt, the smartest man I'll ever know and the only reason I can program at all. Thank you so very much for your patience over the past _decade_ of mentorship you've provided me.
