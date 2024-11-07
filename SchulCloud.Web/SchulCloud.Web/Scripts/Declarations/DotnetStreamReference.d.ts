/**
 * A stream transferred from .NET to js via SignalR.
 */
export declare interface DotNetStreamReference {

    /**
     * Gets the stream as an array buffer.
     */
    arrayBuffer(): Promise<ArrayBuffer>
}