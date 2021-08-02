# Deployment
To clone this folder independently, run the following commands:
```sh
mkdir universalis
cd universalis
git init
git remote add origin https://github.com/Universalis-FFXIV/Universalis
git fetch origin
git checkout origin/v2 -- deployment
```

To update this folder, run the following commands in the root of the git repository:
```sh
git fetch
git reset --hard
git checkout origin/v2 -- deployment
````

## Lumina
This application requires sheets from the game to be installed in `/home/universalis/sqpack`.
Information about this is located [here](https://lumina.xiv.dev/docs/guides/basic-usage.html).