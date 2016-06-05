using cn.bmob.io;
using System;

namespace ConsoleNewsCircle
{
    class NewsList : BmobTable
    {
        public String title { get; set; }
        public String url { get; set; }
        public String img { get; set; }
        public String author { get; set; }
        public String newsType { get; set; }
        public String newsContent { get; set; }
        public long time { get; set; }
        public String newsTime { get; set; }
        public String newsUrl { get; set; }

        public override string table
        {
            get
            {
                return base.table;
            }
        }

        // 读字段信息
        public override void readFields(BmobInput input)
        {
            base.readFields(input);

            this.title = input.getString("newsTitle");
            this.url = input.getString("newsUrl");
            this.img = input.getString("picUrl");
            this.author = input.getString("newsAuthor");
            this.newsType = input.getString("newsType");
        }

        // 写字段信息
        public override void write(BmobOutput output, bool all)
        {
            base.write(output, all);

            output.Put("newsTitle", this.title);
            output.Put("newsUrl", this.url);
            output.Put("picUrl", this.img);
            output.Put("newsAuthor", this.author);
            output.Put("newsType", this.newsType);
            output.Put("newsContent", this.newsContent);
            
        }
    }
}
