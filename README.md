[![Documentation](https://img.shields.io/badge/docs-here-informational)](https://universalis.app/docs)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/f328839ff36f47f7a5672856740d9c00)](https://app.codacy.com/gh/Universalis-FFXIV/Universalis?utm_source=github.com&utm_medium=referral&utm_content=Universalis-FFXIV/Universalis&utm_campaign=Badge_Grade_Settings)
[![Security Headers](https://img.shields.io/security-headers?url=https%3A%2F%2Funiversalis.app)](https://securityheaders.com/?q=https%3A%2F%2Funiversalis.app&followRedirects=on)

# Universalis

A crowdsourced market board aggregator for the game FINAL FANTASY XIV.

## API Reference
Please refer to the [documentation](https://universalis.app/docs) for basic usage information.

## API Development
Developing and testing the API server requires [Visual Studio 2022 Preview](https://docs.microsoft.com/en-us/visualstudio/releases/2022/release-notes-preview), as it targets .NET 6. It also requires [MongoDB Community Edition v4.2](https://docs.mongodb.com/manual/administration/install-community/) or higher.

This application uses some F# code, which needs to be built before IntelliSense can navigate it. If you get any undefined references to F# code, just build the `Universalis.DataTransformations` project.

## Frontend Development
The frontend is housed on our [mogboard fork](https://github.com/Universalis-FFXIV/mogboard), where contributions are welcome.

## Upload Software Development
Please see goat's [ACT plugin](https://github.com/goaaats/universalis_act_plugin) for an example of how to collect and upload market board data.

## Development
Requires .NET 6, [PHP](https://www.php.net/downloads.php), [MariaDB](https://mariadb.org/download/), [Redis](https://redis.io/download), [Composer](https://getcomposer.org/), and [MongoDB Community Edition](https://docs.mongodb.com/manual/administration/install-community/) v4.2 or higher.

Also build a DataExports and an icon2x by running the exporter solution.

Uncomment/add in php.ini:
```
extension=redis.so
```

MariaDB commands:
```
CREATE DATABASE `dalamud`;
CREATE USER 'dalamud'@localhost IDENTIFIED BY 'dalamud';
```

Setup script (mogboard):
```
composer install
php bin/console doctrine:schema:create
php bin/console PopulateGameDataCommand -vvv
php bin/console ImportTranslationsCommand -vvv
yarn
yarn dev
symfony server:start -vvv --port 8000
cd ..
npm run build
npm start
```

## To update
Go to the mogboard/ folder, and execute the following commands after adding any new front-end data.
```
sudo rm -rf var/
sudo redis-cli FLUSHALL
sudo php bin/console PopulateGameDataCommand -vvv
sudo php bin/console ImportTranslationsCommand -vvv
sudo chmod 0777 var/ -R
```

### Single-line form
```
sudo rm -rf var/ && sudo redis-cli FLUSHALL && sudo php bin/console PopulateGameDataCommand -vvv && sudo php bin/console ImportTranslationsCommand -vvv && sudo chmod 0777 var/ -R
```
