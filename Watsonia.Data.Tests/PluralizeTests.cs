﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Watsonia.Data.Tests
{
	[TestClass]
	public class PluralizeTests
	{
		[TestMethod]
		public void TestEasyNouns()
		{
			// Easy (i.e. they follow rules) nouns culled from http://www.manythings.org/vocabulary/lists/l/words.php?f=ogden-general_things
			Assert.AreEqual("accounts", Pluralizer.Pluralize("account"));
			Assert.AreEqual("acts", Pluralizer.Pluralize("act"));
			Assert.AreEqual("additions", Pluralizer.Pluralize("addition"));
			Assert.AreEqual("adjustments", Pluralizer.Pluralize("adjustment"));
			Assert.AreEqual("advertisements", Pluralizer.Pluralize("advertisement"));
			Assert.AreEqual("agreements", Pluralizer.Pluralize("agreement"));
			Assert.AreEqual("airs", Pluralizer.Pluralize("air"));
			Assert.AreEqual("amounts", Pluralizer.Pluralize("amount"));
			Assert.AreEqual("amusements", Pluralizer.Pluralize("amusement"));
			Assert.AreEqual("animals", Pluralizer.Pluralize("animal"));
			Assert.AreEqual("answers", Pluralizer.Pluralize("answer"));
			Assert.AreEqual("apparatuses", Pluralizer.Pluralize("apparatus"));
			Assert.AreEqual("approvals", Pluralizer.Pluralize("approval"));
			Assert.AreEqual("arguments", Pluralizer.Pluralize("argument"));
			Assert.AreEqual("arts", Pluralizer.Pluralize("art"));
			Assert.AreEqual("attacks", Pluralizer.Pluralize("attack"));
			Assert.AreEqual("attempts", Pluralizer.Pluralize("attempt"));
			Assert.AreEqual("attentions", Pluralizer.Pluralize("attention"));
			Assert.AreEqual("attractions", Pluralizer.Pluralize("attraction"));
			Assert.AreEqual("authorities", Pluralizer.Pluralize("authority"));
			Assert.AreEqual("backs", Pluralizer.Pluralize("back"));
			Assert.AreEqual("balances", Pluralizer.Pluralize("balance"));
			Assert.AreEqual("bases", Pluralizer.Pluralize("base"));
			Assert.AreEqual("behaviors", Pluralizer.Pluralize("behavior"));
			Assert.AreEqual("births", Pluralizer.Pluralize("birth"));
			Assert.AreEqual("bits", Pluralizer.Pluralize("bit"));
			Assert.AreEqual("bites", Pluralizer.Pluralize("bite"));
			Assert.AreEqual("bloods", Pluralizer.Pluralize("blood"));
			Assert.AreEqual("blows", Pluralizer.Pluralize("blow"));
			Assert.AreEqual("bodies", Pluralizer.Pluralize("body"));
			Assert.AreEqual("brasses", Pluralizer.Pluralize("brass"));
			Assert.AreEqual("breads", Pluralizer.Pluralize("bread"));
			Assert.AreEqual("breaths", Pluralizer.Pluralize("breath"));
			Assert.AreEqual("brothers", Pluralizer.Pluralize("brother"));
			Assert.AreEqual("buildings", Pluralizer.Pluralize("building"));
			Assert.AreEqual("burns", Pluralizer.Pluralize("burn"));
			Assert.AreEqual("bursts", Pluralizer.Pluralize("burst"));
			Assert.AreEqual("businesses", Pluralizer.Pluralize("business"));
			Assert.AreEqual("butters", Pluralizer.Pluralize("butter"));
			Assert.AreEqual("cares", Pluralizer.Pluralize("care"));
			Assert.AreEqual("causes", Pluralizer.Pluralize("cause"));
			Assert.AreEqual("chalks", Pluralizer.Pluralize("chalk"));
			Assert.AreEqual("chances", Pluralizer.Pluralize("chance"));
			Assert.AreEqual("changes", Pluralizer.Pluralize("change"));
			Assert.AreEqual("cloths", Pluralizer.Pluralize("cloth"));
			Assert.AreEqual("coals", Pluralizer.Pluralize("coal"));
			Assert.AreEqual("colors", Pluralizer.Pluralize("color"));
			Assert.AreEqual("comforts", Pluralizer.Pluralize("comfort"));
			Assert.AreEqual("committees", Pluralizer.Pluralize("committee"));
			Assert.AreEqual("companies", Pluralizer.Pluralize("company"));
			Assert.AreEqual("comparisons", Pluralizer.Pluralize("comparison"));
			Assert.AreEqual("competitions", Pluralizer.Pluralize("competition"));
			Assert.AreEqual("conditions", Pluralizer.Pluralize("condition"));
			Assert.AreEqual("connections", Pluralizer.Pluralize("connection"));
			Assert.AreEqual("controls", Pluralizer.Pluralize("control"));
			Assert.AreEqual("cooks", Pluralizer.Pluralize("cook"));
			Assert.AreEqual("coppers", Pluralizer.Pluralize("copper"));
			Assert.AreEqual("copies", Pluralizer.Pluralize("copy"));
			Assert.AreEqual("corks", Pluralizer.Pluralize("cork"));
			Assert.AreEqual("cottons", Pluralizer.Pluralize("cotton"));
			Assert.AreEqual("coughs", Pluralizer.Pluralize("cough"));
			Assert.AreEqual("countries", Pluralizer.Pluralize("country"));
			Assert.AreEqual("covers", Pluralizer.Pluralize("cover"));
			Assert.AreEqual("cracks", Pluralizer.Pluralize("crack"));
			Assert.AreEqual("credits", Pluralizer.Pluralize("credit"));
			Assert.AreEqual("crimes", Pluralizer.Pluralize("crime"));
			Assert.AreEqual("crushes", Pluralizer.Pluralize("crush"));
			Assert.AreEqual("cries", Pluralizer.Pluralize("cry"));
			Assert.AreEqual("currents", Pluralizer.Pluralize("current"));
			Assert.AreEqual("curves", Pluralizer.Pluralize("curve"));
			Assert.AreEqual("damages", Pluralizer.Pluralize("damage"));
			Assert.AreEqual("dangers", Pluralizer.Pluralize("danger"));
			Assert.AreEqual("daughters", Pluralizer.Pluralize("daughter"));
			Assert.AreEqual("days", Pluralizer.Pluralize("day"));
			Assert.AreEqual("deaths", Pluralizer.Pluralize("death"));
			Assert.AreEqual("debts", Pluralizer.Pluralize("debt"));
			Assert.AreEqual("decisions", Pluralizer.Pluralize("decision"));
			Assert.AreEqual("degrees", Pluralizer.Pluralize("degree"));
			Assert.AreEqual("designs", Pluralizer.Pluralize("design"));
			Assert.AreEqual("desires", Pluralizer.Pluralize("desire"));
			Assert.AreEqual("destructions", Pluralizer.Pluralize("destruction"));
			Assert.AreEqual("details", Pluralizer.Pluralize("detail"));
			Assert.AreEqual("developments", Pluralizer.Pluralize("development"));
			Assert.AreEqual("digestions", Pluralizer.Pluralize("digestion"));
			Assert.AreEqual("directions", Pluralizer.Pluralize("direction"));
			Assert.AreEqual("discoveries", Pluralizer.Pluralize("discovery"));
			Assert.AreEqual("discussions", Pluralizer.Pluralize("discussion"));
			Assert.AreEqual("diseases", Pluralizer.Pluralize("disease"));
			Assert.AreEqual("disgusts", Pluralizer.Pluralize("disgust"));
			Assert.AreEqual("distances", Pluralizer.Pluralize("distance"));
			Assert.AreEqual("distributions", Pluralizer.Pluralize("distribution"));
			Assert.AreEqual("divisions", Pluralizer.Pluralize("division"));
			Assert.AreEqual("doubts", Pluralizer.Pluralize("doubt"));
			Assert.AreEqual("drinks", Pluralizer.Pluralize("drink"));
			Assert.AreEqual("drivings", Pluralizer.Pluralize("driving"));
			Assert.AreEqual("dusts", Pluralizer.Pluralize("dust"));
			Assert.AreEqual("earths", Pluralizer.Pluralize("earth"));
			Assert.AreEqual("edges", Pluralizer.Pluralize("edge"));
			Assert.AreEqual("educations", Pluralizer.Pluralize("education"));
			Assert.AreEqual("effects", Pluralizer.Pluralize("effect"));
			Assert.AreEqual("ends", Pluralizer.Pluralize("end"));
			Assert.AreEqual("errors", Pluralizer.Pluralize("error"));
			Assert.AreEqual("events", Pluralizer.Pluralize("event"));
			Assert.AreEqual("examples", Pluralizer.Pluralize("example"));
			Assert.AreEqual("exchanges", Pluralizer.Pluralize("exchange"));
			Assert.AreEqual("existences", Pluralizer.Pluralize("existence"));
			Assert.AreEqual("expansions", Pluralizer.Pluralize("expansion"));
			Assert.AreEqual("experiences", Pluralizer.Pluralize("experience"));
			Assert.AreEqual("experts", Pluralizer.Pluralize("expert"));
			Assert.AreEqual("facts", Pluralizer.Pluralize("fact"));
			Assert.AreEqual("falls", Pluralizer.Pluralize("fall"));
			Assert.AreEqual("families", Pluralizer.Pluralize("family"));
			Assert.AreEqual("fathers", Pluralizer.Pluralize("father"));
			Assert.AreEqual("fears", Pluralizer.Pluralize("fear"));
			Assert.AreEqual("feelings", Pluralizer.Pluralize("feeling"));
			Assert.AreEqual("fictions", Pluralizer.Pluralize("fiction"));
			Assert.AreEqual("fields", Pluralizer.Pluralize("field"));
			Assert.AreEqual("fights", Pluralizer.Pluralize("fight"));
			Assert.AreEqual("fires", Pluralizer.Pluralize("fire"));
			Assert.AreEqual("flames", Pluralizer.Pluralize("flame"));
			Assert.AreEqual("flights", Pluralizer.Pluralize("flight"));
			Assert.AreEqual("flowers", Pluralizer.Pluralize("flower"));
			Assert.AreEqual("folds", Pluralizer.Pluralize("fold"));
			Assert.AreEqual("foods", Pluralizer.Pluralize("food"));
			Assert.AreEqual("forces", Pluralizer.Pluralize("force"));
			Assert.AreEqual("forms", Pluralizer.Pluralize("form"));
			Assert.AreEqual("friends", Pluralizer.Pluralize("friend"));
			Assert.AreEqual("fronts", Pluralizer.Pluralize("front"));
			Assert.AreEqual("fruits", Pluralizer.Pluralize("fruit"));
			Assert.AreEqual("glasses", Pluralizer.Pluralize("glass"));
			Assert.AreEqual("golds", Pluralizer.Pluralize("gold"));
			Assert.AreEqual("governments", Pluralizer.Pluralize("government"));
			Assert.AreEqual("grains", Pluralizer.Pluralize("grain"));
			Assert.AreEqual("grasses", Pluralizer.Pluralize("grass"));
			Assert.AreEqual("grips", Pluralizer.Pluralize("grip"));
			Assert.AreEqual("groups", Pluralizer.Pluralize("group"));
			Assert.AreEqual("growths", Pluralizer.Pluralize("growth"));
			Assert.AreEqual("guides", Pluralizer.Pluralize("guide"));
			Assert.AreEqual("harbors", Pluralizer.Pluralize("harbor"));
			Assert.AreEqual("harmonies", Pluralizer.Pluralize("harmony"));
			Assert.AreEqual("hates", Pluralizer.Pluralize("hate"));
			Assert.AreEqual("hearings", Pluralizer.Pluralize("hearing"));
			Assert.AreEqual("heats", Pluralizer.Pluralize("heat"));
			Assert.AreEqual("helps", Pluralizer.Pluralize("help"));
			Assert.AreEqual("histories", Pluralizer.Pluralize("history"));
			Assert.AreEqual("holes", Pluralizer.Pluralize("hole"));
			Assert.AreEqual("hopes", Pluralizer.Pluralize("hope"));
			Assert.AreEqual("hours", Pluralizer.Pluralize("hour"));
			Assert.AreEqual("humors", Pluralizer.Pluralize("humor"));
			Assert.AreEqual("ices", Pluralizer.Pluralize("ice"));
			Assert.AreEqual("ideas", Pluralizer.Pluralize("idea"));
			Assert.AreEqual("impulses", Pluralizer.Pluralize("impulse"));
			Assert.AreEqual("increases", Pluralizer.Pluralize("increase"));
			Assert.AreEqual("industries", Pluralizer.Pluralize("industry"));
			Assert.AreEqual("inks", Pluralizer.Pluralize("ink"));
			Assert.AreEqual("insects", Pluralizer.Pluralize("insect"));
			Assert.AreEqual("instruments", Pluralizer.Pluralize("instrument"));
			Assert.AreEqual("insurances", Pluralizer.Pluralize("insurance"));
			Assert.AreEqual("interests", Pluralizer.Pluralize("interest"));
			Assert.AreEqual("inventions", Pluralizer.Pluralize("invention"));
			Assert.AreEqual("irons", Pluralizer.Pluralize("iron"));
			Assert.AreEqual("jellies", Pluralizer.Pluralize("jelly"));
			Assert.AreEqual("joins", Pluralizer.Pluralize("join"));
			Assert.AreEqual("journeys", Pluralizer.Pluralize("journey"));
			Assert.AreEqual("judges", Pluralizer.Pluralize("judge"));
			Assert.AreEqual("jumps", Pluralizer.Pluralize("jump"));
			Assert.AreEqual("kicks", Pluralizer.Pluralize("kick"));
			Assert.AreEqual("kisses", Pluralizer.Pluralize("kiss"));
			Assert.AreEqual("knowledges", Pluralizer.Pluralize("knowledge"));
			Assert.AreEqual("lands", Pluralizer.Pluralize("land"));
			Assert.AreEqual("languages", Pluralizer.Pluralize("language"));
			Assert.AreEqual("laughs", Pluralizer.Pluralize("laugh"));
			Assert.AreEqual("laws", Pluralizer.Pluralize("law"));
			Assert.AreEqual("leads", Pluralizer.Pluralize("lead"));
			Assert.AreEqual("learnings", Pluralizer.Pluralize("learning"));
			Assert.AreEqual("leathers", Pluralizer.Pluralize("leather"));
			Assert.AreEqual("letters", Pluralizer.Pluralize("letter"));
			Assert.AreEqual("levels", Pluralizer.Pluralize("level"));
			Assert.AreEqual("lifts", Pluralizer.Pluralize("lift"));
			Assert.AreEqual("lights", Pluralizer.Pluralize("light"));
			Assert.AreEqual("limits", Pluralizer.Pluralize("limit"));
			Assert.AreEqual("linens", Pluralizer.Pluralize("linen"));
			Assert.AreEqual("liquids", Pluralizer.Pluralize("liquid"));
			Assert.AreEqual("lists", Pluralizer.Pluralize("list"));
			Assert.AreEqual("looks", Pluralizer.Pluralize("look"));
			Assert.AreEqual("losses", Pluralizer.Pluralize("loss"));
			Assert.AreEqual("loves", Pluralizer.Pluralize("love"));
			Assert.AreEqual("machines", Pluralizer.Pluralize("machine"));
			Assert.AreEqual("managers", Pluralizer.Pluralize("manager"));
			Assert.AreEqual("marks", Pluralizer.Pluralize("mark"));
			Assert.AreEqual("markets", Pluralizer.Pluralize("market"));
			Assert.AreEqual("masses", Pluralizer.Pluralize("mass"));
			Assert.AreEqual("meals", Pluralizer.Pluralize("meal"));
			Assert.AreEqual("measures", Pluralizer.Pluralize("measure"));
			Assert.AreEqual("meats", Pluralizer.Pluralize("meat"));
			Assert.AreEqual("meetings", Pluralizer.Pluralize("meeting"));
			Assert.AreEqual("memories", Pluralizer.Pluralize("memory"));
			Assert.AreEqual("metals", Pluralizer.Pluralize("metal"));
			Assert.AreEqual("middles", Pluralizer.Pluralize("middle"));
			Assert.AreEqual("milks", Pluralizer.Pluralize("milk"));
			Assert.AreEqual("minds", Pluralizer.Pluralize("mind"));
			Assert.AreEqual("mines", Pluralizer.Pluralize("mine"));
			Assert.AreEqual("minutes", Pluralizer.Pluralize("minute"));
			Assert.AreEqual("mists", Pluralizer.Pluralize("mist"));
			Assert.AreEqual("moneys", Pluralizer.Pluralize("money"));
			Assert.AreEqual("months", Pluralizer.Pluralize("month"));
			Assert.AreEqual("mornings", Pluralizer.Pluralize("morning"));
			Assert.AreEqual("mothers", Pluralizer.Pluralize("mother"));
			Assert.AreEqual("motions", Pluralizer.Pluralize("motion"));
			Assert.AreEqual("mountains", Pluralizer.Pluralize("mountain"));
			Assert.AreEqual("moves", Pluralizer.Pluralize("move"));
			Assert.AreEqual("musics", Pluralizer.Pluralize("music"));
			Assert.AreEqual("names", Pluralizer.Pluralize("name"));
			Assert.AreEqual("nations", Pluralizer.Pluralize("nation"));
			Assert.AreEqual("needs", Pluralizer.Pluralize("need"));
			Assert.AreEqual("news", Pluralizer.Pluralize("news"));
			Assert.AreEqual("nights", Pluralizer.Pluralize("night"));
			Assert.AreEqual("noises", Pluralizer.Pluralize("noise"));
			Assert.AreEqual("notes", Pluralizer.Pluralize("note"));
			Assert.AreEqual("numbers", Pluralizer.Pluralize("number"));
			Assert.AreEqual("observations", Pluralizer.Pluralize("observation"));
			Assert.AreEqual("offers", Pluralizer.Pluralize("offer"));
			Assert.AreEqual("oils", Pluralizer.Pluralize("oil"));
			Assert.AreEqual("operations", Pluralizer.Pluralize("operation"));
			Assert.AreEqual("opinions", Pluralizer.Pluralize("opinion"));
			Assert.AreEqual("orders", Pluralizer.Pluralize("order"));
			Assert.AreEqual("organisations", Pluralizer.Pluralize("organisation"));
			Assert.AreEqual("ornaments", Pluralizer.Pluralize("ornament"));
			Assert.AreEqual("owners", Pluralizer.Pluralize("owner"));
			Assert.AreEqual("pages", Pluralizer.Pluralize("page"));
			Assert.AreEqual("pains", Pluralizer.Pluralize("pain"));
			Assert.AreEqual("paints", Pluralizer.Pluralize("paint"));
			Assert.AreEqual("papers", Pluralizer.Pluralize("paper"));
			Assert.AreEqual("parts", Pluralizer.Pluralize("part"));
			Assert.AreEqual("pastes", Pluralizer.Pluralize("paste"));
			Assert.AreEqual("payments", Pluralizer.Pluralize("payment"));
			Assert.AreEqual("peaces", Pluralizer.Pluralize("peace"));
			Assert.AreEqual("persons", Pluralizer.Pluralize("person"));
			Assert.AreEqual("places", Pluralizer.Pluralize("place"));
			Assert.AreEqual("plants", Pluralizer.Pluralize("plant"));
			Assert.AreEqual("plays", Pluralizer.Pluralize("play"));
			Assert.AreEqual("pleasures", Pluralizer.Pluralize("pleasure"));
			Assert.AreEqual("points", Pluralizer.Pluralize("point"));
			Assert.AreEqual("poisons", Pluralizer.Pluralize("poison"));
			Assert.AreEqual("polishes", Pluralizer.Pluralize("polish"));
			Assert.AreEqual("porters", Pluralizer.Pluralize("porter"));
			Assert.AreEqual("positions", Pluralizer.Pluralize("position"));
			Assert.AreEqual("powders", Pluralizer.Pluralize("powder"));
			Assert.AreEqual("powers", Pluralizer.Pluralize("power"));
			Assert.AreEqual("prices", Pluralizer.Pluralize("price"));
			Assert.AreEqual("prints", Pluralizer.Pluralize("print"));
			Assert.AreEqual("processes", Pluralizer.Pluralize("process"));
			Assert.AreEqual("produces", Pluralizer.Pluralize("produce"));
			Assert.AreEqual("profits", Pluralizer.Pluralize("profit"));
			Assert.AreEqual("properties", Pluralizer.Pluralize("property"));
			Assert.AreEqual("proses", Pluralizer.Pluralize("prose"));
			Assert.AreEqual("protests", Pluralizer.Pluralize("protest"));
			Assert.AreEqual("pulls", Pluralizer.Pluralize("pull"));
			Assert.AreEqual("punishments", Pluralizer.Pluralize("punishment"));
			Assert.AreEqual("purposes", Pluralizer.Pluralize("purpose"));
			Assert.AreEqual("pushes", Pluralizer.Pluralize("push"));
			Assert.AreEqual("qualities", Pluralizer.Pluralize("quality"));
			Assert.AreEqual("questions", Pluralizer.Pluralize("question"));
			Assert.AreEqual("rains", Pluralizer.Pluralize("rain"));
			Assert.AreEqual("ranges", Pluralizer.Pluralize("range"));
			Assert.AreEqual("rates", Pluralizer.Pluralize("rate"));
			Assert.AreEqual("rays", Pluralizer.Pluralize("ray"));
			Assert.AreEqual("reactions", Pluralizer.Pluralize("reaction"));
			Assert.AreEqual("readings", Pluralizer.Pluralize("reading"));
			Assert.AreEqual("reasons", Pluralizer.Pluralize("reason"));
			Assert.AreEqual("records", Pluralizer.Pluralize("record"));
			Assert.AreEqual("regrets", Pluralizer.Pluralize("regret"));
			Assert.AreEqual("relations", Pluralizer.Pluralize("relation"));
			Assert.AreEqual("religions", Pluralizer.Pluralize("religion"));
			Assert.AreEqual("representatives", Pluralizer.Pluralize("representative"));
			Assert.AreEqual("requests", Pluralizer.Pluralize("request"));
			Assert.AreEqual("respects", Pluralizer.Pluralize("respect"));
			Assert.AreEqual("rests", Pluralizer.Pluralize("rest"));
			Assert.AreEqual("rewards", Pluralizer.Pluralize("reward"));
			Assert.AreEqual("rhythms", Pluralizer.Pluralize("rhythm"));
			Assert.AreEqual("rices", Pluralizer.Pluralize("rice"));
			Assert.AreEqual("rivers", Pluralizer.Pluralize("river"));
			Assert.AreEqual("roads", Pluralizer.Pluralize("road"));
			Assert.AreEqual("rolls", Pluralizer.Pluralize("roll"));
			Assert.AreEqual("rooms", Pluralizer.Pluralize("room"));
			Assert.AreEqual("rubs", Pluralizer.Pluralize("rub"));
			Assert.AreEqual("rules", Pluralizer.Pluralize("rule"));
			Assert.AreEqual("runs", Pluralizer.Pluralize("run"));
			Assert.AreEqual("salts", Pluralizer.Pluralize("salt"));
			Assert.AreEqual("sands", Pluralizer.Pluralize("sand"));
			Assert.AreEqual("scales", Pluralizer.Pluralize("scale"));
			Assert.AreEqual("sciences", Pluralizer.Pluralize("science"));
			Assert.AreEqual("seas", Pluralizer.Pluralize("sea"));
			Assert.AreEqual("seats", Pluralizer.Pluralize("seat"));
			Assert.AreEqual("secretaries", Pluralizer.Pluralize("secretary"));
			Assert.AreEqual("selections", Pluralizer.Pluralize("selection"));
			Assert.AreEqual("senses", Pluralizer.Pluralize("sense"));
			Assert.AreEqual("servants", Pluralizer.Pluralize("servant"));
			Assert.AreEqual("sexes", Pluralizer.Pluralize("sex"));
			Assert.AreEqual("shades", Pluralizer.Pluralize("shade"));
			Assert.AreEqual("shakes", Pluralizer.Pluralize("shake"));
			Assert.AreEqual("shames", Pluralizer.Pluralize("shame"));
			Assert.AreEqual("shocks", Pluralizer.Pluralize("shock"));
			Assert.AreEqual("sides", Pluralizer.Pluralize("side"));
			Assert.AreEqual("signs", Pluralizer.Pluralize("sign"));
			Assert.AreEqual("silks", Pluralizer.Pluralize("silk"));
			Assert.AreEqual("silvers", Pluralizer.Pluralize("silver"));
			Assert.AreEqual("sisters", Pluralizer.Pluralize("sister"));
			Assert.AreEqual("sizes", Pluralizer.Pluralize("size"));
			Assert.AreEqual("skies", Pluralizer.Pluralize("sky"));
			Assert.AreEqual("sleeps", Pluralizer.Pluralize("sleep"));
			Assert.AreEqual("slips", Pluralizer.Pluralize("slip"));
			Assert.AreEqual("slopes", Pluralizer.Pluralize("slope"));
			Assert.AreEqual("smashes", Pluralizer.Pluralize("smash"));
			Assert.AreEqual("smells", Pluralizer.Pluralize("smell"));
			Assert.AreEqual("smiles", Pluralizer.Pluralize("smile"));
			Assert.AreEqual("smokes", Pluralizer.Pluralize("smoke"));
			Assert.AreEqual("sneezes", Pluralizer.Pluralize("sneeze"));
			Assert.AreEqual("snows", Pluralizer.Pluralize("snow"));
			Assert.AreEqual("soaps", Pluralizer.Pluralize("soap"));
			Assert.AreEqual("societies", Pluralizer.Pluralize("society"));
			Assert.AreEqual("sons", Pluralizer.Pluralize("son"));
			Assert.AreEqual("songs", Pluralizer.Pluralize("song"));
			Assert.AreEqual("sorts", Pluralizer.Pluralize("sort"));
			Assert.AreEqual("sounds", Pluralizer.Pluralize("sound"));
			Assert.AreEqual("soups", Pluralizer.Pluralize("soup"));
			Assert.AreEqual("spaces", Pluralizer.Pluralize("space"));
			Assert.AreEqual("stages", Pluralizer.Pluralize("stage"));
			Assert.AreEqual("starts", Pluralizer.Pluralize("start"));
			Assert.AreEqual("statements", Pluralizer.Pluralize("statement"));
			Assert.AreEqual("steams", Pluralizer.Pluralize("steam"));
			Assert.AreEqual("steels", Pluralizer.Pluralize("steel"));
			Assert.AreEqual("steps", Pluralizer.Pluralize("step"));
			Assert.AreEqual("stitches", Pluralizer.Pluralize("stitch"));
			Assert.AreEqual("stones", Pluralizer.Pluralize("stone"));
			Assert.AreEqual("stops", Pluralizer.Pluralize("stop"));
			Assert.AreEqual("stories", Pluralizer.Pluralize("story"));
			Assert.AreEqual("stretches", Pluralizer.Pluralize("stretch"));
			Assert.AreEqual("structures", Pluralizer.Pluralize("structure"));
			Assert.AreEqual("substances", Pluralizer.Pluralize("substance"));
			Assert.AreEqual("sugars", Pluralizer.Pluralize("sugar"));
			Assert.AreEqual("suggestions", Pluralizer.Pluralize("suggestion"));
			Assert.AreEqual("summers", Pluralizer.Pluralize("summer"));
			Assert.AreEqual("supports", Pluralizer.Pluralize("support"));
			Assert.AreEqual("surprises", Pluralizer.Pluralize("surprise"));
			Assert.AreEqual("swims", Pluralizer.Pluralize("swim"));
			Assert.AreEqual("systems", Pluralizer.Pluralize("system"));
			Assert.AreEqual("talks", Pluralizer.Pluralize("talk"));
			Assert.AreEqual("tastes", Pluralizer.Pluralize("taste"));
			Assert.AreEqual("taxes", Pluralizer.Pluralize("tax"));
			Assert.AreEqual("teachings", Pluralizer.Pluralize("teaching"));
			Assert.AreEqual("tendencies", Pluralizer.Pluralize("tendency"));
			Assert.AreEqual("tests", Pluralizer.Pluralize("test"));
			Assert.AreEqual("theories", Pluralizer.Pluralize("theory"));
			Assert.AreEqual("things", Pluralizer.Pluralize("thing"));
			Assert.AreEqual("thoughts", Pluralizer.Pluralize("thought"));
			Assert.AreEqual("thunders", Pluralizer.Pluralize("thunder"));
			Assert.AreEqual("times", Pluralizer.Pluralize("time"));
			Assert.AreEqual("tins", Pluralizer.Pluralize("tin"));
			Assert.AreEqual("tops", Pluralizer.Pluralize("top"));
			Assert.AreEqual("touches", Pluralizer.Pluralize("touch"));
			Assert.AreEqual("trades", Pluralizer.Pluralize("trade"));
			Assert.AreEqual("transports", Pluralizer.Pluralize("transport"));
			Assert.AreEqual("tricks", Pluralizer.Pluralize("trick"));
			Assert.AreEqual("troubles", Pluralizer.Pluralize("trouble"));
			Assert.AreEqual("turns", Pluralizer.Pluralize("turn"));
			Assert.AreEqual("twists", Pluralizer.Pluralize("twist"));
			Assert.AreEqual("units", Pluralizer.Pluralize("unit"));
			Assert.AreEqual("uses", Pluralizer.Pluralize("use"));
			Assert.AreEqual("values", Pluralizer.Pluralize("value"));
			Assert.AreEqual("verses", Pluralizer.Pluralize("verse"));
			Assert.AreEqual("vessels", Pluralizer.Pluralize("vessel"));
			Assert.AreEqual("views", Pluralizer.Pluralize("view"));
			Assert.AreEqual("voices", Pluralizer.Pluralize("voice"));
			Assert.AreEqual("walks", Pluralizer.Pluralize("walk"));
			Assert.AreEqual("wars", Pluralizer.Pluralize("war"));
			Assert.AreEqual("washes", Pluralizer.Pluralize("wash"));
			Assert.AreEqual("wastes", Pluralizer.Pluralize("waste"));
			Assert.AreEqual("waters", Pluralizer.Pluralize("water"));
			Assert.AreEqual("waves", Pluralizer.Pluralize("wave"));
			Assert.AreEqual("waxes", Pluralizer.Pluralize("wax"));
			Assert.AreEqual("ways", Pluralizer.Pluralize("way"));
			Assert.AreEqual("weathers", Pluralizer.Pluralize("weather"));
			Assert.AreEqual("weeks", Pluralizer.Pluralize("week"));
			Assert.AreEqual("weights", Pluralizer.Pluralize("weight"));
			Assert.AreEqual("winds", Pluralizer.Pluralize("wind"));
			Assert.AreEqual("wines", Pluralizer.Pluralize("wine"));
			Assert.AreEqual("winters", Pluralizer.Pluralize("winter"));
			Assert.AreEqual("woods", Pluralizer.Pluralize("wood"));
			Assert.AreEqual("wools", Pluralizer.Pluralize("wool"));
			Assert.AreEqual("words", Pluralizer.Pluralize("word"));
			Assert.AreEqual("works", Pluralizer.Pluralize("work"));
			Assert.AreEqual("wounds", Pluralizer.Pluralize("wound"));
			Assert.AreEqual("writings", Pluralizer.Pluralize("writing"));
			Assert.AreEqual("years", Pluralizer.Pluralize("year"));
		}

		[TestMethod]
		public void TestHarderNouns()
		{
			Assert.AreEqual("branches", Pluralizer.Pluralize("branch"));
			Assert.AreEqual("foxes", Pluralizer.Pluralize("fox"));
			Assert.AreEqual("buses", Pluralizer.Pluralize("bus"));
			Assert.AreEqual("buses", Pluralizer.Pluralize("bus"));
			Assert.AreEqual("halves", Pluralizer.Pluralize("half"));
			Assert.AreEqual("leaves", Pluralizer.Pluralize("leaf"));
			Assert.AreEqual("thieves", Pluralizer.Pluralize("thief"));
			Assert.AreEqual("wolves", Pluralizer.Pluralize("wolf"));
			Assert.AreEqual("knives", Pluralizer.Pluralize("knife"));
			Assert.AreEqual("lives", Pluralizer.Pluralize("life"));
		}

		[TestMethod]
		public void TestExceptions()
		{
			Assert.AreEqual("men", Pluralizer.Pluralize("man"));
			Assert.AreEqual("women", Pluralizer.Pluralize("woman"));
		}
	}
}
