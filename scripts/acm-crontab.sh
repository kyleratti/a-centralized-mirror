#!/bin/bash
curl -X POST -H "X-ACM-Crontab-Token: $API_CRONTAB_TOKEN" http://127.0.0.1:$PORT/cron/commentreply/sync
