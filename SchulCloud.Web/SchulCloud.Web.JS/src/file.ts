import { dotNetStreamReference } from "./declarations/dotnetStreamReference";

export namespace file {
    export async function download(fileName: string, streamReference: dotNetStreamReference): Promise<void> {
        const arrayBuffer: ArrayBuffer = await streamReference.arrayBuffer();
        const blob: Blob = new Blob([arrayBuffer]);

        const url = URL.createObjectURL(blob);
        downloadFromUrl(fileName, url);
        URL.revokeObjectURL(url);
    }

    export function downloadFromUrl(fileName: string, url: string): void {
        const anchorElement = document.createElement('a');

        anchorElement.href = url;
        anchorElement.download = fileName;
        anchorElement.target = "_self";

        anchorElement.click();
        anchorElement.remove();
    }
}