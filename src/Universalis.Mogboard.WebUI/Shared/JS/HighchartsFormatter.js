class HighchartsFormatter
{
    /**
     * Formats extended history data from Universalis into a Highcharts-compatible format.
     * @param {Object} extendedHistory - The API response data.
     * @param {Object} labels - The chart labels.
     */
    formatExtendedHistory(extendedHistory, labels)
    {
        // We could just take entries directly, but this gets us better typechecking
        const data = [...extendedHistory.entries]
            .map(entry =>
            {
                return {
                    hq: !!entry.hq,
                    date: entry.timestamp * 1000,
                    value: Math.ceil(entry.pricePerUnity),

                    // If the quantity is missing (e.g. entries before December 2019) or 0 (??), we just set it to 1
                    qty: Math.ceil(!entry.quantity ? 1 : entry.quantity),
                };
            });

        const hqData = [];
        const nqData = [];
        const hqDataVolume = [];
        const nqDataVolume = [];

        for (const entry of data)
        {
            const arr = entry.hq ? hqData : nqData;
            const volumeArr = entry.hq ? hqDataVolume : nqDataVolume;

            if (arr[entry.date])
            {
                // Increment values
                arr[entry.date][1] += entry.value;
                volumeArr[entry.date][1] += entry.qty;
            }
            else
            {
                // Set date and values
                arr[entry.date] = [entry.date, entry.value];
                volumeArr[entry.date] = [entry.date, entry.qty];
            }
        }

        // Sort arrays
        hqData.sort((a, b) => a[0] - b[0]);
        nqData.sort((a, b) => a[0] - b[0]);
        hqDataVolume.sort((a, b) => a[0] - b[0]);
        nqDataVolume.sort((a, b) => a[0] - b[0]);

        return {
            series: [
                {
                    id: "HC_History_HQ",
                    name: labels.hqPpu,
                    data: hqData,
                    yAxis: 0,
                    showInNavigator: true,
                    negativeColor: true,
                    navigatorOptions: {
                        type: "line",
                        color: "rgba(202,200,68,0.35)",
                    },
                },
                {
                    id: "HC_History_NQ",
                    name: labels.nqPpu,
                    data: nqData,
                    yAxis: 0,
                    showInNavigator: true,
                    negativeColor: true,
                    navigatorOptions: {
                        type: "line",
                        color: "rgba(120,120,120,0.35)",
                    },
                },
                {
                    id:   "HC_History_HQ_volume",
                    name: labels.hqPpuQuantity,
                    data: hqDataVolume,
                    linkedTo: "HC_History_HQ",
                    type: "column",
                    yAxis: 1,
                    showInNavigator: false,
                },
                {
                    id:   "HC_History_NQ_volume",
                    name: labels.nqPpuQuantity,
                    data: nqDataVolume,
                    linkedTo: "HC_History_NQ",
                    type: "column",
                    yAxis: 1,
                    showInNavigator: false,
                }
            ],
        };
    }
}

export default new HighchartsFormatter;
