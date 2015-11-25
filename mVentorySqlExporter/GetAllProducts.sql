
declare @scs as table (
	product_id int primary key, 
	barcode nvarchar(15), 
	shortdesc nvarchar(40), 
	longdesc nvarchar(255), 
	cat1 nvarchar(6), 
	cat2 nvarchar(6), 
	cat3 nvarchar(6), 
	custom1 nvarchar(50), 
	allow_fractions int,
	quantity float,
	price money,
	branches nvarchar(255)
)

-- create a unique list of products
insert  into @scs (product_id, quantity) select master_id, sum(floor(quantity)) from Info_Stock where floor(quantity)>0 or date_modified>'{0}'  group by master_id 

-- update common details
update @scs set barcode = ist.barcode, shortdesc = ist.description, longdesc = ist.longdesc, cat1 = ist.cat1, cat2 = ist.cat2,
cat3 = ist.cat3, custom1 = ist.custom1, allow_fractions = ist.allow_fractions, price=ist.sell
from @scs scs, Info_Stock ist 
where ist.master_id=scs.product_id

-- update quantities per branch
-- loop though every branch and contatenate names of branches where the product is stocked
declare @branchid int, @branchname nvarchar(500)
declare branch_cursor CURSOR FOR select shop_id, shop_name from Master_ShopList
open branch_cursor
FETCH NEXT FROM branch_cursor into @branchid, @branchname
WHILE @@FETCH_STATUS = 0
 BEGIN
	update @scs set branches=isnull(branches,'') + isnull(@branchname,'') + ':' + convert(nvarchar(20),isnull(floor(ist.quantity),0)) + ';'  
		from @scs scs, Info_Stock ist where scs.product_id=ist.master_id and ist.linked_shop=@branchid and floor(ist.quantity)>0
	FETCH NEXT FROM branch_cursor into @branchid, @branchname
 END
CLOSE branch_cursor
DEALLOCATE branch_cursor

-- return data
select * from @scs order by product_id desc

