module.exports.levenshtein = (input: string, test: string) => {
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
};

export default module.exports;
