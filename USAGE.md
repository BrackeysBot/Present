# Slash Commands

Below is an outline of every slash command currently implemented in Present, along with their descriptions and parameters.

## User Blocking

If you wish for a certain user to be discarded as a potential winner of giveaways, you can block them using the following command.
This does not prevent the user from joining the giveaway, but they will not be considered when determining a winner.

### `/giveaway blockuser`

Prevent a user from winning giveaways.

| Parameter | Required | Type               | Description               |
|:----------|:---------|:-------------------|:--------------------------|
| user      | ✅ Yes    | User mention or ID | The user to block.        |
| reason    | ❌ No     | String             | The reason for the block. |

### `/giveaway unblockuser`

Unblocks a user, so that they are able to win giveaways.

| Parameter | Required | Type               | Description          |
|:----------|:---------|:-------------------|:---------------------|
| user      | ✅ Yes    | User mention or ID | The user to unblock. |

## Role Blocking

If you wish for members with a certain role to be discarded as potential winners of giveaways, you can block them using the
following command.
This does not prevent members with that role from joining the giveaway, but they will not be considered when determining a winner.

### `/giveaway blockrole`

Prevent members with the role from winning giveaways.

| Parameter | Required | Type               | Description               |
|:----------|:---------|:-------------------|:--------------------------|
| role      | ✅ Yes    | Role mention or ID | The role to block.        |
| reason    | ❌ No     | String             | The reason for the block. |

### `/giveaway unblockrole`

Unblocks a role, so that members with that role are able to win giveaways.

| Parameter | Required | Type               | Description          |
|:----------|:---------|:-------------------|:---------------------|
| role      | ✅ Yes    | Role mention or ID | The role to unblock. |

## Giveaway Management

### `/giveaway create`

Creates a new giveaway.

| Parameter   | Required | Type                  | Description                                       |
|:------------|:---------|:----------------------|:--------------------------------------------------|
| channel     | ✅ Yes    | Channel mention or ID | The channel in which the giveaway will be hosted. |
| winnerCount | ✅ Yes    | Integer               | The number of winners for this giveaway.          |

Following this command, a modal will appear in which you can specify the giveaway's title, description, end time, and image URL.
The end time can either be a relative time (e.g. 1w3d) or a Unix timestamp in seconds (e.g. 1734998400).
The title cannot exceed 255 characters, and the description cannot exceed 4000 characters.
The image URL must be a valid URI, or an empty string.

### `/giveaway end`

Prematurely ends an ongoing giveaway. This command bypasses the winner selection.

| Parameter | Required | Type      | Description                    |
|:----------|:---------|:----------|:-------------------------------|
| id        | ✅ Yes    | ShortGuid | The ID of the giveaway to end. |

### `/giveaway view`

Views the details of a giveaway.

| Parameter | Required | Type      | Description                     |
|:----------|:---------|:----------|:--------------------------------|
| id        | ✅ Yes    | ShortGuid | The ID of the giveaway to view. |

# Ephemeral responses

None of the commands respond ephemerally, unless an error occurs.
