# Architecture

- `adr/`: architecture decision records.
- `adr/0005-shared-core-upm-package.md`: shared core source as a local UPM package.
- `c4/`: reserved for C4 diagrams and context maps.

Current target:

```text
Unity Android/Windows client
            |
            v
   BFF / API gateway
    /       |        \
Identity   Game   Telemetry worker
    \       |        /
     PostgreSQL / RabbitMQ
```

Start with a modular backend. Extract services only when a documented need appears.
