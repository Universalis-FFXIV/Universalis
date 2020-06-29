import { RemoteDataManager } from "./remote/RemoteDataManager";

export async function initializeWorldMappings(
	remoteDataManager: RemoteDataManager,
	worldMap: Map<string, number>,
	worldIDMap: Map<number, string>,
) {
	const registerWorld = bindMaps(worldMap, worldIDMap);

	const worldList = await remoteDataManager.parseCSV("World.csv");
	for (const worldEntry of worldList) {
		if (
			!parseInt(worldEntry[0]) ||
			worldEntry[0] === "25" ||
			worldEntry[4] === "False"
		)
			continue;
		registerWorld(worldEntry[1], parseInt(worldEntry[0]));
	}

	// CN needs some custom stuff so people can look up world data in both Chinese and romanized Chinese
	registerWorld("红玉海", 1167);
	registerWorld("神意之地", 1081);
	registerWorld("拉诺西亚", 1042);
	registerWorld("幻影群岛", 1044);
	registerWorld("萌芽池", 1060);
	registerWorld("宇宙和音", 1173);
	registerWorld("沃仙曦染", 1174);
	registerWorld("晨曦王座", 1175);
	registerWorld("白银乡", 1172);
	registerWorld("白金幻象", 1076);
	registerWorld("神拳痕", 1171);
	registerWorld("潮风亭", 1170);
	registerWorld("旅人栈桥", 1113);
	registerWorld("拂晓之间", 1121);
	registerWorld("龙巢神殿", 1166);
	registerWorld("梦羽宝境", 1176);
	registerWorld("紫水栈桥", 1043);
	registerWorld("延夏", 1169);
	registerWorld("静语庄园", 1106);
	registerWorld("摩杜纳", 1045);
	registerWorld("海猫茶屋", 1177);
	registerWorld("柔风海湾", 1178);
	registerWorld("琥珀原", 1179);
}

function bindMaps(
	worldMap: Map<string, number>,
	worldIDMap: Map<number, string>,
): (name: string, id: number) => void {
	return (name, id) => _registerWorld(name, id, worldMap, worldIDMap);
}

function _registerWorld(
	name: string,
	id: number,
	worldMap: Map<string, number>,
	worldIDMap: Map<number, string>,
) {
	worldMap.set(name, id);
	worldIDMap.set(id, name);
}
