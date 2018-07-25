# a-centralized-mirror
This is a centralized API and mirror bot for subs that utilize multiple mirror bots.

## How it works
Typically, subreddits want mirror links stickied to the top of each thread so visitors can find live mirrors without having to scroll through all of the comments. Unfortunately, reddit limits sticky comments to one per thread, so only one mirror can appear on the top.

**a-mirror-bot** will maintain a single stickied comment with a list of links to the mirrors. Bots can update their status whenever and the bot will update the link accordingly.

## Limitations
Subreddits must make **a-mirror-bot** a moderator for this bot to work. Without this in place, reddit's spam filters and rate limiters will make the bot almost useless. Also, the bot won't be able to sticky its own posts, so...what's the point?

# Subreddit mods
If your sub is interested in having its mirror bots use **a-mirror-bot** to maintain a single stickied list of mirrors, please [contact the developer](https://reddit.com/message/compose/?to=Clutch_22&subject=a-mirror-bot%20-%20new%20subreddit%20support).

# Bot devleopers
Our API is very straightforward to use (or at least we think so). Please [see our wiki](https://github.com/kyleratti/a-centralized-mirror/wiki) for more details.

You will need to [contact the developer](https://reddit.com/message/compose/?to=Clutch_22&subject=a-mirror-bot%20-%20api%20access) to receive your API access tokens and register your bot. In your message, please include:

```
bot username:
developer:
active subreddits:
do the mods of those subreddits know about this bot?
```
