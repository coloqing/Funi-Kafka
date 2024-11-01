using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DataBase
{
    public abstract class BaseEntity
    {
        /// <summary>
        /// ID
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [JsonIgnore]
        [MaxLength(50)]
        public int? CreateUserId { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        [JsonIgnore]
        [MaxLength(50)]
        public int? UpdateUserId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonIgnore]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 修改时间
        /// </summary>
        [JsonIgnore]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否删除 0：未删除 1：已删除
        /// </summary>
        [JsonIgnore]
        public bool IsDeleted { get; set; }
    }
}