# Monitoring

A Docker Compose setup for MariaDB, Grafana, and Prometheus.

## Data

All tables are seeded with a row with an ID of `00000000-0000-0000-0000-000000000000`. The seeded user has a session token of `xxxxxxxxxxxxxxxxxxxxxxxx`. There is not currently an option in the API explorer to provide the session token as a cookie, but this can still be tested using cURL using the `--cookie` parameter.