import { existsSync, readFileSync, readdirSync, unlinkSync, writeFileSync } from "fs";
import { join } from "path"

const inputPath = join(__dirname, "..", "..", "src", "endpoints");
const outputFile = join(__dirname, "..", "..", "public", "json", "docs.json");

if (existsSync(outputFile))
    unlinkSync(outputFile);

const endpoints = readdirSync(inputPath);

const output: DocInfo[] = [];

endpoints.forEach(file => {
    const data = readFileSync(join(inputPath, file)).toString();
    output.push(readMarkup(data));
});

writeFileSync(outputFile, JSON.stringify(output));

function readMarkup(data: string): DocInfo {
    const markupComment = data.substring(0, data.indexOf("*/"))
        .split(/\r\n/g)
        .filter((line) => line.indexOf("@") !== -1)
        .map((line) => {
            const thing = line.slice(line.indexOf("@") + 1);
            const things = thing.split(" ");
            return [things[0], thing.slice(things[0].length + 1)];
        });
    console.log(markupComment);

    let url: string;
    let params: Parameter[] = [];
    let disabled = false;

    markupComment.forEach((line) => {
        const directive = line[0];
        const value = line[1];

        if (directive === "url")
            url = value;
        else if (directive === "param") {
            const name = value.split(" ")[0];
            params.push({
                name: name,
                usage: value.slice(name.length + 1)
            });
        }
        else if (directive === "disabled")
            disabled = (value === "true" ? true : false);
    });

    return {
        url,
        params,
        disabled
    };
}

interface DocInfo {
    url: string;
    params: Parameter[];
    disabled: boolean;
}

interface Parameter {
    name: string;
    usage: string;
}