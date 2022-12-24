<h1 align="center">Present</h1>
<p align="center"><img src="icon.png" width="128"></p>
<p align="center"><i>A Discord bot for managing giveaways.</i></p>
<p align="center">
<a href="https://github.com/BrackeysBot/Present/releases"><img src="https://img.shields.io/github/v/release/BrackeysBot/Present?include_prereleases"></a>
<a href="https://github.com/BrackeysBot/Present/actions?query=workflow%3A%22.NET%22"><img src="https://img.shields.io/github/actions/workflow/status/BrackeysBot/Present/dotnet.yml" alt="GitHub Workflow Status" title="GitHub Workflow Status"></a>
<a href="https://github.com/BrackeysBot/Present/issues"><img src="https://img.shields.io/github/issues/BrackeysBot/Present" alt="GitHub Issues" title="GitHub Issues"></a>
<a href="https://github.com/BrackeysBot/Present/blob/main/LICENSE.md"><img src="https://img.shields.io/github/license/BrackeysBot/Present" alt="MIT License" title="MIT License"></a>
</p>

## About
Present is a Discord bot which manages giveaways in your server. It is capable of allowing multiple winners, as well as excluding specific users and roles form being able to win.

## Installing and configuring Present 
Present runs in a Docker container, and there is a [docker-compose.yaml](docker-compose.yaml) file which simplifies this process.

### Clone the repository
To start off, clone the repository into your desired directory:
```bash
git clone https://github.com/BrackeysBot/Present.git
```
Step into the Present directory using `cd Present`, and continue with the steps below.

### Setting things up
The bot's token is passed to the container using the `DISCORD_TOKEN` environment variable. Define this environment variable whichever way is most convenient for you.

Two directories are required to exist for Docker compose to mount as container volumes. A folder for persistent data, and a folder for logs:
```bash
sudo mkdir /etc/brackeysbot/present
sudo mkdir /var/log/brackeysbot/present
```
Copy the example `config.example.json` to `/etc/brackeysbot/present/config.json`, and assign the necessary config keys. Below is breakdown of the config.json layout:
```json
{
  "GUILD_ID": {
    "logChannel": /* The ID of the log channel */,
    "giveawayColor": /* The primary branding colour, as a 24-bit RGB integer. Defaults to #7837FF */
  }
}
```
The `logs` directory is used to store logs in a format similar to that of a Minecraft server. `latest.log` will contain the log for the current day and current execution. All past logs are archived.

The `data` directory is used to store persistent state of the bot, such as config values and the infraction database.

### Launch Present
To launch Present, simply run the following commands:
```bash
sudo docker-compose build
sudo docker-compose up --detach
```

## Updating Present
To update Present, simply pull the latest changes from the repo and restart the container:
```bash
git pull
sudo docker-compose stop
sudo docker-compose build
sudo docker-compose up --detach
```

## Using Present
For further usage breakdown and explanation of commands, see [USAGE.md](USAGE.md).

## License
This bot is under the [MIT License](LICENSE.md).

## Disclaimer
This bot is tailored for use within the [Brackeys Discord server](https://discord.gg/brackeys). While this bot is open source and you are free to use it in your own servers, you accept responsibility for any mishaps which may arise from the use of this software. Use at your own risk.
