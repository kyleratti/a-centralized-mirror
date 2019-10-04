#!/bin/bash
curl -X POST -H "X-ACM-Crontab-Token: $API_CRONTAB_TOKEN" http://web:$PORT/cron/commentreply/sync
