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
    let returns: Return[] = [];
    let experimental = false;
    let disabled = false;

    markupComment.forEach((line) => {
        const directive = line[0];
        const value = line[1];

        if (directive === "url")
            url = value;
        else if (directive === "param") {
            const words = value.split(" ");
            const name = words.shift();
            let type = words.shift();
            while (words[0] === "|") {
                type += ` ${words.shift()} ${words.shift()}`;
            }
            params.push({
                name,
                type,
                usage: value.slice(name.length + type.length + 2),
            });
        }
        else if (directive === "returns") {
            const words = value.split(" ");
            const name = words.shift();
            let type = words.shift();
            while (words[0] === "|") {
                type += ` ${words.shift()} ${words.shift()}`;
            }
            returns.push({
                name,
                type,
                usage: value.slice(name.length + type.length + 2),
            });
        }
        else if (directive === "experimental")
            experimental = true;
        else if (directive === "disabled")
            disabled = true;
    });

    return {
        url,
        params,
        returns,
        experimental,
        disabled
    };
}

interface DocInfo {
    url: string;
    params: Parameter[];
    returns: Return[];
    experimental: boolean;
    disabled: boolean;
}

interface Parameter {
    name: string;
    type: string;
    usage: string;
}

interface Return {
    name: string;
    type: string;
    usage: string;
}