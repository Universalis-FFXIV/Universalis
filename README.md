[![Documentation](https://img.shields.io/badge/docs-here-informational)](https://universalis.app/docs)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/f328839ff36f47f7a5672856740d9c00)](https://app.codacy.com/gh/Universalis-FFXIV/Universalis?utm_source=github.com&utm_medium=referral&utm_content=Universalis-FFXIV/Universalis&utm_campaign=Badge_Grade_Settings)
[![Security Headers](https://img.shields.io/security-headers?url=https%3A%2F%2Funiversalis.app)](https://securityheaders.com/?q=https%3A%2F%2Funiversalis.app&followRedirects=on)

# Universalis

A crowdsourced market board aggregator for the game FINAL FANTASY XIV.

## API Reference
Please refer to the [documentation](https://universalis.app/docs) for basic usage information.

## API Development
Developing and testing the API server requires [Visual Studio 2022 Preview](https://docs.microsoft.com/en-us/visualstudio/releases/2022/release-notes-preview), as it targets .NET 6.

This application uses some F# code, which needs to be built before IntelliSense can navigate it. If you get any undefined references to F# code, just build the `Universalis.DataTransformations` project.

## Frontend Development
The frontend is housed on our [mogboard repo](https://github.com/Universalis-FFXIV/mogboard-next), where contributions are welcome.

## Upload Software Development
Please see goat's [ACT plugin](https://github.com/goaaats/universalis_act_plugin) for an example of how to collect and upload market board data.

## Development
Requires .NET 6, [PHP 7.2](https://www.php.net/downloads.php), PostgreSQL [MariaDB](https://mariadb.org/download/), [Redis](https://redis.io/download), and [Composer 1](https://getcomposer.org/). A development environment is provided as a Docker Compose specification in the `devenv` folder.

To compile web assets in the new web project, install the `WebPack Task Runner` extension in Visual Studio and run `npm install` in the web UI project directory.

Also build a DataExports and an icon2x by running the exporter solution.

Uncomment/add in php.ini:
```ini
extension=redis.so
```

MariaDB commands:
```mysql
CREATE DATABASE `dalamud`;
CREATE USER 'dalamud'@localhost IDENTIFIED BY 'dalamud';
GRANT ALL PRIVILEGES ON `dalamud`.* TO 'dalamud'@localhost IDENTIFIED BY 'dalamud';
FLUSH PRIVILEGES;
```

Setup script (mogboard):
```bash
composer install
php bin/console doctrine:schema:create
php bin/console PopulateGameDataCommand -vvv
php bin/console ImportTranslationsCommand -vvv
yarn
yarn dev
symfony server:start -vvv --port 8000
```

## To update
Go to the mogboard/ folder, and execute the following commands after adding any new front-end data.
```bash
sudo rm -rf var/
sudo redis-cli FLUSHALL
sudo php bin/console PopulateGameDataCommand -vvv
sudo php bin/console ImportTranslationsCommand -vvv
sudo chmod 0777 var/ -R
```

### Single-line form
```bash
sudo rm -rf var/ && sudo redis-cli FLUSHALL && sudo php bin/console PopulateGameDataCommand -vvv && sudo php bin/console ImportTranslationsCommand -vvv && sudo chmod 0777 var/ -R
```
