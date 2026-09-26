using Newtonsoft.Json;

namespace AvatarAnimator.Deserialize
{
    public static class AvatarAnimatorDataDeserializer
    {
        public static AvatarAnimatorData Deserialize(string version, string data)
        {
            if (null == data || "" == data) throw new Exception("Cannot Deserialize null or empty data");
            return version switch
            {
                AvatarAnimatorDataContainer.m_CurrentVersion => JsonConvert.DeserializeObject<AvatarAnimatorData>(data),
                _ => throw new Exception($"Unsuported Data version '{version}'"),
            };
        }
    }
}
