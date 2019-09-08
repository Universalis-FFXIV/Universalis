import remoteDataManager from "./remoteDataManager";

export async function getWorldDC(world: string) {
    const dataCenterWorlds = JSON.parse((await remoteDataManager.fetchFile("dc.json")).toString());
    for (let dc in dataCenterWorlds) {
        if (dataCenterWorlds.hasOwnProperty(dc)) {
            let foundWorld = dataCenterWorlds[dc].find((el: string) => el === world);
            if (foundWorld) return dc;
        }
    }
    return undefined;
}

export async function getWorldName(worldID: number) {
    const worldCSV = (await remoteDataManager.parseCSV("World.csv")).slice(3);
    const world = worldCSV.find((line: string[]) => line[0] === String(worldID))[1];
    return world;
}

export function levenshtein(input: string, test: string) {
    if (input.length === 0) return test.length; // Edge cases
    if (test.length === 0) return input.length;

    if (input === test) return 0; // Easy case

    let matrix: number[][] = []; // Setting up matrix

    for (var n = 0; n <= test.length; n++) { // y-axis
        matrix[n] = [];
        matrix[n][0] = n;
    }

    for (var m = 0; m <= input.length; m++) { // x-axis
        matrix[0][m] = m;
    }

    // Calculation to fill out the matrix
    for (var i = 1; i <= test.length; i++) {
        for (var j = 1; j <= input.length; j++) {
            if (test[i] === input[j]) { // It takes 0 changes to turn a letter into itself
                matrix[i][j] = matrix[i - 1][j - 1];
                continue;
            }

            matrix[i][j] = Math.min(matrix[i - 1][j], matrix[i][j - 1], matrix[i - 1][j - 1]) + 1;
        }
    }

    return matrix[test.length][input.length]; // The total cost is described in the last element of the matrix
}

export function stringifyContentIDs(jsonString: string) {
    const parameters = ["sellerID", "buyerID", "creatorID"];

    parameters.forEach((parameter) => {
        let paramIndex = jsonString.indexOf(parameter);

        if (paramIndex === -1) return;

        paramIndex += parameter.length + 2;
        while (jsonString.charAt(paramIndex) === " ") paramIndex++;

        let endIndex = jsonString.substr(paramIndex).indexOf(",");
        if (endIndex === -1) {
            endIndex = jsonString.substr(paramIndex).indexOf(" ");
        }
        if (endIndex === -1) {
            endIndex = jsonString.substr(paramIndex).indexOf("}");
        }

        jsonString = jsonString.substr(0, paramIndex) + "\"" +
                     jsonString.substr(paramIndex, endIndex).trim() + "\"" +
                     jsonString.substr(endIndex);
    });

    return jsonString;
}
