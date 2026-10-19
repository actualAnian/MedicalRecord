using Newtonsoft.Json;

namespace Register.DTO
{
    /*
     * http://hl7.org/fhir/valueset-icd-10.xml.html
        <jurisdiction> 
            <coding> 
              <system value="http://unstats.un.org/unsd/methods/m49/m49.htm"/> 
              <code value="001"/> 
              <display value="World"/> 
            </coding> 
        </jurisdiction>
     */

    // list of codes:
    // https://hl7.org/fhir/6.0.0-snapshot1/valueset-icd-10.html
    public record ICD10(
        [property: JsonProperty("system")] string System,
        [property: JsonProperty("code")] string Code,
        [property: JsonProperty("display")] string Display);

    public static class ICD10Reader
    {
        public static List<ICD10>? Read(string filePath)
        {
            string json = File.ReadAllText(filePath);

            return JsonConvert.DeserializeObject<List<ICD10>>(json);
        }
    }
}
